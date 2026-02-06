using FluentAssertions;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.Persistence.Specifications;

namespace TrackYourDay.Tests.Persistence
{
    public class AndSpecificationTests
    {
        [Fact]
        public void GivenTwoSpecifications_WhenBothSatisfied_ThenCombinedSpecificationReturnsTrue()
        {
            // Given
            var break1 = new EndedBreak(
                Guid.NewGuid(),
                new DateTime(2026, 2, 6, 10, 0, 0),
                new DateTime(2026, 2, 6, 10, 15, 0),
                "Test break");

            var dateSpec = new BreakByDateSpecification(new DateOnly(2026, 2, 6));
            var nonRevokedSpec = new NonRevokedBreaksSpecification();
            var andSpec = new AndSpecification<EndedBreak>(dateSpec, nonRevokedSpec);

            // When
            var result = andSpec.IsSatisfiedBy(break1);

            // Then
            result.Should().BeTrue();
        }

        [Fact]
        public void GivenTwoSpecifications_WhenFirstNotSatisfied_ThenCombinedSpecificationReturnsFalse()
        {
            // Given
            var break1 = new EndedBreak(
                Guid.NewGuid(),
                new DateTime(2026, 2, 5, 10, 0, 0), // Different date
                new DateTime(2026, 2, 5, 10, 15, 0),
                "Test break");

            var dateSpec = new BreakByDateSpecification(new DateOnly(2026, 2, 6));
            var nonRevokedSpec = new NonRevokedBreaksSpecification();
            var andSpec = new AndSpecification<EndedBreak>(dateSpec, nonRevokedSpec);

            // When
            var result = andSpec.IsSatisfiedBy(break1);

            // Then
            result.Should().BeFalse();
        }

        [Fact]
        public void GivenTwoSpecifications_WhenSecondNotSatisfied_ThenCombinedSpecificationReturnsFalse()
        {
            // Given
            var break1 = new EndedBreak(
                Guid.NewGuid(),
                new DateTime(2026, 2, 6, 10, 0, 0),
                new DateTime(2026, 2, 6, 10, 15, 0),
                "Test break")
            {
                RevokedAt = DateTime.Now // Revoked
            };

            var dateSpec = new BreakByDateSpecification(new DateOnly(2026, 2, 6));
            var nonRevokedSpec = new NonRevokedBreaksSpecification();
            var andSpec = new AndSpecification<EndedBreak>(dateSpec, nonRevokedSpec);

            // When
            var result = andSpec.IsSatisfiedBy(break1);

            // Then
            result.Should().BeFalse();
        }

        [Fact]
        public void GivenTwoSpecifications_WhenNeitherSatisfied_ThenCombinedSpecificationReturnsFalse()
        {
            // Given
            var break1 = new EndedBreak(
                Guid.NewGuid(),
                new DateTime(2026, 2, 5, 10, 0, 0), // Wrong date
                new DateTime(2026, 2, 5, 10, 15, 0),
                "Test break")
            {
                RevokedAt = DateTime.Now // And revoked
            };

            var dateSpec = new BreakByDateSpecification(new DateOnly(2026, 2, 6));
            var nonRevokedSpec = new NonRevokedBreaksSpecification();
            var andSpec = new AndSpecification<EndedBreak>(dateSpec, nonRevokedSpec);

            // When
            var result = andSpec.IsSatisfiedBy(break1);

            // Then
            result.Should().BeFalse();
        }

        [Fact]
        public void WhenGettingSqlWhereClause_ThenCombinesBothClausesWithAnd()
        {
            // Given
            var dateSpec = new BreakByDateSpecification(new DateOnly(2026, 2, 6));
            var nonRevokedSpec = new NonRevokedBreaksSpecification();
            var andSpec = new AndSpecification<EndedBreak>(dateSpec, nonRevokedSpec);

            // When
            var sqlClause = andSpec.GetSqlWhereClause();

            // Then
            sqlClause.Should().Contain("date(json_extract(DataJson, '$.BreakStartedAt')) = @date");
            sqlClause.Should().Contain("json_extract(DataJson, '$.RevokedAt') IS NULL");
            sqlClause.Should().Contain(" AND ");
        }

        [Fact]
        public void WhenGettingSqlParameters_ThenMergesParametersFromBothSpecifications()
        {
            // Given
            var dateSpec = new BreakByDateSpecification(new DateOnly(2026, 2, 6));
            var nonRevokedSpec = new NonRevokedBreaksSpecification();
            var andSpec = new AndSpecification<EndedBreak>(dateSpec, nonRevokedSpec);

            // When
            var parameters = andSpec.GetSqlParameters();

            // Then
            parameters.Should().ContainKey("@date");
            parameters["@date"].Should().Be("2026-02-06");
        }

        [Fact]
        public void GivenNullLeftSpecification_WhenCreatingAndSpec_ThenThrowsArgumentNullException()
        {
            // Given
            var nonRevokedSpec = new NonRevokedBreaksSpecification();

            // When
            Action act = () => new AndSpecification<EndedBreak>(null!, nonRevokedSpec);

            // Then
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("left");
        }

        [Fact]
        public void GivenNullRightSpecification_WhenCreatingAndSpec_ThenThrowsArgumentNullException()
        {
            // Given
            var dateSpec = new BreakByDateSpecification(new DateOnly(2026, 2, 6));

            // When
            Action act = () => new AndSpecification<EndedBreak>(dateSpec, null!);

            // Then
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("right");
        }
    }
}
