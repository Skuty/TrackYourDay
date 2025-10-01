using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Core.SystemTrackers.SystemStates;

// Helper class for ML.NET pipeline
public class TextData
{
    public string Text { get; set; }
}

// Helper class for ML.NET pipeline
public class TextFeatures
{
    [VectorType(300)] // Fixed size vector for word embeddings
    public float[] Features { get; set; }
}

namespace TrackYourDay.Core.Insights.Analytics
{
    public class SummaryGenerator : IDisposable
    {
        private readonly ILogger<SummaryGenerator> _logger;
        private readonly MLContext _mlContext;
        private readonly ITransformer _textFeaturizer;
        private readonly ConcurrentDictionary<string, GroupedActivity> _activityGroups;
        private readonly IClock _clock;
        private bool _disposed = false;

        public SummaryGenerator(IClock clock, ILogger<SummaryGenerator> logger)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activityGroups = new ConcurrentDictionary<string, GroupedActivity>();
            
            // Initialize ML.NET context
            _mlContext = new MLContext(seed: 1);
            
            // Create a text featurization pipeline
            var textPipeline = _mlContext.Transforms.Text.NormalizeText("NormalizedText", "Text")
                .Append(_mlContext.Transforms.Text.TokenizeIntoWords("Tokens", "NormalizedText"))
                .Append(_mlContext.Transforms.Text.RemoveDefaultStopWords("Tokens"))
                // Use WordEmbeddings instead of N-grams for better semantic understanding
                .Append(_mlContext.Transforms.Text.ProduceWordBags("Features", "Tokens",
                    ngramLength: 1, // Use unigrams for simplicity
                    useAllLengths: false,
                    weighting: NgramExtractingEstimator.WeightingCriteria.TfIdf));

            // Create a small dataset to fit the pipeline
            var emptyData = _mlContext.Data.LoadFromEnumerable(new[] { new TextData { Text = "" } });
            _textFeaturizer = textPipeline.Fit(emptyData);
        }

        public IReadOnlyCollection<GroupedActivity> Generate(IEnumerable<EndedActivity> activities)
        {
            if (activities == null) throw new ArgumentNullException(nameof(activities));

            var activitiesList = activities.ToList();
            if (!activitiesList.Any())
            {
                _logger.LogInformation("No activities to generate summary for.");
                return Array.Empty<GroupedActivity>();
            }

            // Group activities by date
            var activitiesByDate = activitiesList
                .GroupBy(a => DateOnly.FromDateTime(a.StartDate.Date))
                .OrderBy(g => g.Key);

            var result = new List<GroupedActivity>();

            foreach (var dailyActivities in activitiesByDate)
            {
                var date = dailyActivities.Key;
                var dailyActivitiesList = dailyActivities.OrderBy(a => a.StartDate).ToList();

                // Process activities to find logical groups
                var groupedActivities = ProcessActivities(dailyActivitiesList);

                // Add to results
                result.AddRange(groupedActivities.Values);
            }

            return result.AsReadOnly();
        }

        private Dictionary<string, GroupedActivity> ProcessActivities(List<EndedActivity> activities)
        {
            var groups = new Dictionary<string, GroupedActivity>();
            var date = DateOnly.FromDateTime(activities.First().StartDate);

            // First pass: Group by exact matches
            foreach (var activity in activities)
            {
                var description = activity.GetDescription();
                var activityType = activity.ActivityType.GetType().Name;
                var groupKey = $"{activityType}_{description}";

                if (!groups.TryGetValue(groupKey, out var group))
                {
                    group = GroupedActivity.CreateEmptyWithDescriptionForDate(date, description);
                    groups[groupKey] = group;
                }

                group.Include(activity.Guid, new TimePeriod(activity.StartDate, activity.EndDate));
            }

            // If we have too many small groups, try to merge them based on semantic similarity
            if (groups.Count > 10)  // Arbitrary threshold
            {
                groups = MergeSimilarGroups(groups);
            }

            return groups;
        }

        private Dictionary<string, GroupedActivity> MergeSimilarGroups(Dictionary<string, GroupedActivity> groups)
        {
            var groupList = groups.Values.ToList();
            var similarityMatrix = CalculateSimilarityMatrix(groupList);
            var mergedGroups = new Dictionary<string, GroupedActivity>();
            var mergedIndices = new HashSet<int>();

            for (int i = 0; i < groupList.Count; i++)
            {
                if (mergedIndices.Contains(i)) continue;

                var currentGroup = groupList[i];
                var currentDescription = currentGroup.Description;
                var currentVector = GetTextVector(currentDescription);
                var similarGroups = new List<GroupedActivity> { currentGroup };

                // Find similar groups
                for (int j = i + 1; j < groupList.Count; j++)
                {
                    if (mergedIndices.Contains(j)) continue;

                    var otherGroup = groupList[j];
                    var similarity = CalculateCosineSimilarity(
                        currentVector, 
                        GetTextVector(otherGroup.Description));

                    // If similarity is above threshold, merge
                    if (similarity > 0.7)  // Threshold can be adjusted
                    {
                        similarGroups.Add(otherGroup);
                        mergedIndices.Add(j);
                    }
                }

                // Merge similar groups
                if (similarGroups.Count > 1)
                {
                    var mergedGroup = MergeGroups(similarGroups);
                    var newKey = $"Merged_{Guid.NewGuid()}";
                    mergedGroups[newKey] = mergedGroup;
                }
                else
                {
                    var key = groups.First(g => g.Value == currentGroup).Key;
                    mergedGroups[key] = currentGroup;
                }
            }
            return mergedGroups;
        }

        private float[] GetTextVector(string text)
        {
            try
            {
                var data = new[] { new TextData { Text = text } };
                var dataView = _mlContext.Data.LoadFromEnumerable(data);
                var transformedData = _textFeaturizer.Transform(dataView);
                
                // Get the feature vector as VBuffer<float> and convert to array
                using (var cursor = transformedData.GetRowCursor(transformedData.Schema.Where(c => c.Name == "Features")))
                {
                    var features = default(VBuffer<float>);
                    var featureColumn = cursor.GetGetter<VBuffer<float>>(transformedData.Schema["Features"]);
                    
                    if (cursor.MoveNext())
                    {
                        featureColumn(ref features);
                        return features.DenseValues().ToArray();
                    }
                }
                
                return Array.Empty<float>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating text vector for: {Text}", text);
                return Array.Empty<float>();
            }
        }

        private float CalculateCosineSimilarity(float[] vector1, float[] vector2)
        {
            if (vector1 == null || vector2 == null || vector1.Length == 0 || vector2.Length == 0)
                return 0f;

            // Ensure vectors are of the same length
            int length = Math.Min(vector1.Length, vector2.Length);
            float dotProduct = 0;
            float magnitude1 = 0;
            float magnitude2 = 0;

            for (int i = 0; i < length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += vector1[i] * vector1[i];
                magnitude2 += vector2[i] * vector2[i];
            }

            magnitude1 = (float)Math.Sqrt(magnitude1);
            magnitude2 = (float)Math.Sqrt(magnitude2);

            if (magnitude1 == 0 || magnitude2 == 0)
                return 0f;

            return dotProduct / (magnitude1 * magnitude2);
        }

        private float[,] CalculateSimilarityMatrix(List<GroupedActivity> groups)
        {
            int count = groups.Count;
            var matrix = new float[count, count];
            var vectors = groups.Select(g => GetTextVector(g.Description)).ToArray();

            for (int i = 0; i < count; i++)
            {
                for (int j = i; j < count; j++)
                {
                    var similarity = CalculateCosineSimilarity(vectors[i], vectors[j]);
                    matrix[i, j] = similarity;
                    if (i != j)
                        matrix[j, i] = similarity;
                }
            }

            return matrix;
        }

        private GroupedActivity MergeGroups(IEnumerable<GroupedActivity> groups)
        {
            var groupList = groups.ToList();
            if (!groupList.Any())
                throw new ArgumentException("No groups to merge");

            var firstGroup = groupList.First();
            var date = firstGroup.Date;
            
            // Find the most representative description (could be the longest or most common)
            var description = groupList
                .OrderByDescending(g => g.Description.Length)
                .First()
                .Description;

            var mergedGroup = GroupedActivity.CreateEmptyWithDescriptionForDate(date, $"Group: {description}");
            
            // Merge all activities from all groups
            foreach (var group in groupList)
            {
                // This is a simplified merge - in a real implementation, you'd want to handle the merging of time periods
                // and ensure no overlaps are double-counted
                mergedGroup.Include(Guid.NewGuid(), new TimePeriod(
                    group.Date.ToDateTime(TimeOnly.MinValue), 
                    group.Date.ToDateTime(TimeOnly.MaxValue)));
            }

            return mergedGroup;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    if (_textFeaturizer is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                _disposed = true;
            }
        }

        ~SummaryGenerator()
        {
            Dispose(false);
        }
    }
}
