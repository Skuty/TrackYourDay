using FluentAssertions;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.Persistence.Specifications;

namespace TrackYourDay.Tests.Persistence
{
    public class NonRevokedBreaksSpecificationTests
    {
        [Fact]
        public void GivenNonRevokedBreak_WhenCheckingIsSatisfiedBy_ThenReturnsTrue()
        {
            // Given
            var endedBreak = new EndedBreak(
                Guid.NewGuid(), 
                new DateTime(2026, 2, 6, 10, 0, 0), 
                new DateTime(2026, 2, 6, 10, 15, 0), 
                "Test break");

            var specification = new NonRevokedBreaksSpecification();

            // When
            var result = specification.IsSatisfiedBy(endedBreak);

            // Then
            result.Should().BeTrue();
        }

        [Fact]
        public void GivenRevokedBreak_WhenCheckingIsSatisfiedBy_ThenReturnsFalse()
        {
            // Given
            var endedBreak = new EndedBreak(
                Guid.NewGuid(), 
                new DateTime(2026, 2, 6, 10, 0, 0), 
                new DateTime(2026, 2, 6, 10, 15, 0), 
                "Test break")
            {
                RevokedAt = new DateTime(2026, 2, 6, 10, 20, 0)
            };

            var specification = new NonRevokedBreaksSpecification();

            // When
            var result = specification.IsSatisfiedBy(endedBreak);

            // Then
            result.Should().BeFalse();
        }

        [Fact]
        public void WhenGettingSqlWhereClause_ThenReturnsCorrectClause()
        {
            // Given
            var specification = new NonRevokedBreaksSpecification();

            // When
            var sqlClause = specification.GetSqlWhereClause();

            // Then
            sqlClause.Should().Be("json_extract(DataJson, '$.RevokedAt') IS NULL");
        }

        [Fact]
        public void WhenGettingSqlParameters_ThenReturnsEmptyDictionary()
        {
            // Given
            var specification = new NonRevokedBreaksSpecification();

            // When
            var parameters = specification.GetSqlParameters();

            // Then
            parameters.Should().BeEmpty();
        }
    }
}
