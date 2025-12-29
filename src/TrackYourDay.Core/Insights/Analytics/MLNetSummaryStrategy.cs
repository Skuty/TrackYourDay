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
using TrackYourDay.Core.Insights.Analytics;

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
    public class MLNetSummaryStrategy : ISummaryStrategy, IDisposable
    {
        private readonly ILogger<MLNetSummaryStrategy> _logger;
        private readonly MLContext _mlContext;
        private readonly ITransformer _textFeaturizer;
        private readonly ConcurrentDictionary<string, GroupedActivity> _activityGroups;
        private readonly IClock _clock;
        private bool _disposed = false;

        public MLNetSummaryStrategy(IClock clock, ILogger<MLNetSummaryStrategy> logger)
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

        public IReadOnlyCollection<GroupedActivity> Generate(IEnumerable<ITrackableItem> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            var itemsList = items.ToList();
            if (!itemsList.Any())
            {
                _logger.LogInformation("No items to generate summary for.");
                return Array.Empty<GroupedActivity>();
            }

            // Group items by date
            var itemsByDate = itemsList
                .GroupBy(a => DateOnly.FromDateTime(a.StartDate.Date))
                .OrderBy(g => g.Key);

            var result = new List<GroupedActivity>();

            foreach (var dailyItems in itemsByDate)
            {
                var date = dailyItems.Key;
                var dailyItemsList = dailyItems.OrderBy(a => a.StartDate).ToList();

                // Process items to find logical groups
                var groupedItems = ProcessItems(dailyItemsList);

                // Add to results
                result.AddRange(groupedItems.Values);
            }

            return result.AsReadOnly();
        }

        private Dictionary<string, GroupedActivity> ProcessItems(List<ITrackableItem> items)
        {
            var groups = new Dictionary<string, GroupedActivity>();
            var date = DateOnly.FromDateTime(items.First().StartDate);

            // First pass: Group by exact matches
            foreach (var item in items)
            {
                var description = item.GetDescription();
                var itemType = item.GetType().Name;
                var groupKey = $"{itemType}_{description}";

                if (!groups.TryGetValue(groupKey, out var group))
                {
                    group = GroupedActivity.CreateEmptyWithDescriptionForDate(date, description);
                    groups[groupKey] = group;
                }

                group.Include(item.Guid, new TimePeriod(item.StartDate, item.EndDate));
            }

            // Apply semantic similarity grouping to merge similar activities
            if (groups.Count > 1)
            {
                groups = MergeSimilarGroups(groups);
            }

            return groups;
        }

        private Dictionary<string, GroupedActivity> MergeSimilarGroups(Dictionary<string, GroupedActivity> groups)
        {
            var groupList = groups.Values.ToList();
            var mergedGroups = new Dictionary<string, GroupedActivity>();
            var mergedIndices = new HashSet<int>();

            for (int i = 0; i < groupList.Count; i++)
            {
                if (mergedIndices.Contains(i)) continue;

                var currentGroup = groupList[i];
                var currentDescription = currentGroup.Description;
                var similarGroups = new List<GroupedActivity> { currentGroup };

                // Find similar groups
                for (int j = i + 1; j < groupList.Count; j++)
                {
                    if (mergedIndices.Contains(j)) continue;

                    var otherGroup = groupList[j];
                    
                    // Use both ML.NET similarity and simple word overlap
                    var mlSimilarity = CalculateCosineSimilarity(
                        GetTextVector(currentDescription), 
                        GetTextVector(otherGroup.Description));
                    
                    var wordSimilarity = CalculateWordOverlapSimilarity(
                        currentDescription, 
                        otherGroup.Description);

                    _logger.LogDebug("Similarity between '{Desc1}' and '{Desc2}': ML={MLSim}, Word={WordSim}", 
                        currentDescription, otherGroup.Description, mlSimilarity, wordSimilarity);

                    // If either similarity metric is above threshold, merge
                    // Lower thresholds to be more aggressive with grouping
                    if (mlSimilarity > 0.3 || wordSimilarity > 0.25)
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

            var mergedGroup = GroupedActivity.CreateEmptyWithDescriptionForDate(date, description);
            
            // Collect all time periods from all groups using the public method
            foreach (var group in groupList)
            {
                var periodsWithEvents = group.GetIncludedPeriodsWithEvents();
                
                foreach (var (eventGuid, period) in periodsWithEvents)
                {
                    mergedGroup.Include(eventGuid, period);
                }
            }

            return mergedGroup;
        }

        private float CalculateWordOverlapSimilarity(string text1, string text2)
        {
            if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
                return 0f;

            // Normalize and tokenize
            var words1 = text1.ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\n', '\r', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2) // Filter out very short words
                .ToList();
            
            var words2 = text2.ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\n', '\r', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToList();

            if (words1.Count == 0 || words2.Count == 0)
                return 0f;

            // Count common words
            var commonWords = words1.Intersect(words2).ToList();
            
            if (commonWords.Count == 0)
                return 0f;

            // Use Dice coefficient instead of Jaccard for better similarity on short texts
            // Dice = 2 * |intersection| / (|A| + |B|)
            var diceCoefficient = (2.0f * commonWords.Count) / (words1.Count + words2.Count);
            
            return diceCoefficient;
        }

        public string StrategyName => "ML.NET Semantic Summary";

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

        ~MLNetSummaryStrategy()
        {
            Dispose(false);
        }
    }
}
