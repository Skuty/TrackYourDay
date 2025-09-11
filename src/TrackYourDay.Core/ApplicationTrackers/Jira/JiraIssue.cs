namespace TrackYourDay.Core.ApplicationTrackers.Jira
{
    public record JiraIssue(
        string Key,
        string Summary,
        DateTime Updated
    );
}