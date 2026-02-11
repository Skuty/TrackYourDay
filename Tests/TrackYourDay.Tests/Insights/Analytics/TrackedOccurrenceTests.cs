using FluentAssertions;
using TrackYourDay.Core.Insights.Analytics;

namespace TrackYourDay.Tests.Insights.Analytics
{
    public class TrackedOccurrenceTests
    {
        [Fact]
        public void GivenEmptyEventId_WhenConstructing_ThenThrowsArgumentException()
        {
            // Given
            var emptyGuid = Guid.Empty;
            var period = new TimePeriod(DateTime.Now, DateTime.Now.AddHours(1));

            // When
            var act = () => new TrackedOccurrence(emptyGuid, period);

            // Then
            act.Should().Throw<ArgumentException>()
                .WithMessage("Event ID cannot be empty.*");
        }

        [Fact]
        public void GivenNullPeriod_WhenConstructing_ThenThrowsArgumentNullException()
        {
            // Given
            var eventId = Guid.NewGuid();

            // When
            var act = () => new TrackedOccurrence(eventId, null);

            // Then
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GivenValidParameters_WhenConstructing_ThenCreatesOccurrence()
        {
            // Given
            var eventId = Guid.NewGuid();
            var startDate = new DateTime(2000, 01, 01, 09, 00, 00);
            var endDate = new DateTime(2000, 01, 01, 10, 00, 00);
            var period = new TimePeriod(startDate, endDate);

            // When
            var occurrence = new TrackedOccurrence(eventId, period);

            // Then
            occurrence.EventId.Should().Be(eventId);
            occurrence.Period.Should().Be(period);
        }

        [Fact]
        public void GivenOverlappingOccurrences_WhenCheckingOverlap_ThenReturnsTrue()
        {
            // Given
            var occurrence1 = new TrackedOccurrence(
                Guid.NewGuid(),
                new TimePeriod(new DateTime(2000, 01, 01, 09, 00, 00), new DateTime(2000, 01, 01, 10, 00, 00))
            );
            var occurrence2 = new TrackedOccurrence(
                Guid.NewGuid(),
                new TimePeriod(new DateTime(2000, 01, 01, 09, 30, 00), new DateTime(2000, 01, 01, 10, 30, 00))
            );

            // When
            var result = occurrence1.OverlapsWith(occurrence2);

            // Then
            result.Should().BeTrue();
        }

        [Fact]
        public void GivenNonOverlappingOccurrences_WhenCheckingOverlap_ThenReturnsFalse()
        {
            // Given
            var occurrence1 = new TrackedOccurrence(
                Guid.NewGuid(),
                new TimePeriod(new DateTime(2000, 01, 01, 09, 00, 00), new DateTime(2000, 01, 01, 10, 00, 00))
            );
            var occurrence2 = new TrackedOccurrence(
                Guid.NewGuid(),
                new TimePeriod(new DateTime(2000, 01, 01, 11, 00, 00), new DateTime(2000, 01, 01, 12, 00, 00))
            );

            // When
            var result = occurrence1.OverlapsWith(occurrence2);

            // Then
            result.Should().BeFalse();
        }

        [Fact]
        public void GivenOccurrenceDuringTimeRange_WhenChecking_ThenReturnsTrue()
        {
            // Given
            var occurrence = new TrackedOccurrence(
                Guid.NewGuid(),
                new TimePeriod(new DateTime(2000, 01, 01, 09, 30, 00), new DateTime(2000, 01, 01, 10, 30, 00))
            );
            var rangeStart = new DateTime(2000, 01, 01, 09, 00, 00);
            var rangeEnd = new DateTime(2000, 01, 01, 11, 00, 00);

            // When
            var result = occurrence.OccurredDuring(rangeStart, rangeEnd);

            // Then
            result.Should().BeTrue();
        }

        [Fact]
        public void GivenOccurrenceOutsideTimeRange_WhenChecking_ThenReturnsFalse()
        {
            // Given
            var occurrence = new TrackedOccurrence(
                Guid.NewGuid(),
                new TimePeriod(new DateTime(2000, 01, 01, 09, 30, 00), new DateTime(2000, 01, 01, 10, 30, 00))
            );
            var rangeStart = new DateTime(2000, 01, 01, 11, 00, 00);
            var rangeEnd = new DateTime(2000, 01, 01, 12, 00, 00);

            // When
            var result = occurrence.OccurredDuring(rangeStart, rangeEnd);

            // Then
            result.Should().BeFalse();
        }

        [Fact]
        public void GivenTwoOccurrencesWithSameData_WhenComparing_ThenAreEqual()
        {
            // Given
            var eventId = Guid.NewGuid();
            var period = new TimePeriod(new DateTime(2000, 01, 01, 09, 00, 00), new DateTime(2000, 01, 01, 10, 00, 00));
            var occurrence1 = new TrackedOccurrence(eventId, period);
            var occurrence2 = new TrackedOccurrence(eventId, period);

            // When/Then
            occurrence1.Should().Be(occurrence2);
        }

        [Fact]
        public void GivenOccurrence_WhenCallingToString_ThenReturnsFormattedString()
        {
            // Given
            var eventId = Guid.Parse("12345678-1234-1234-1234-123456789012");
            var period = new TimePeriod(new DateTime(2000, 01, 01, 09, 15, 00), new DateTime(2000, 01, 01, 10, 30, 00));
            var occurrence = new TrackedOccurrence(eventId, period);

            // When
            var result = occurrence.ToString();

            // Then
            result.Should().Contain("12345678123412341234123456789012");
            result.Should().Contain("09:15");
            result.Should().Contain("10:30");
        }
    }
}
