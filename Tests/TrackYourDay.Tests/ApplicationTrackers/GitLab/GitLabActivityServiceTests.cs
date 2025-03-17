using FluentAssertions;
using Moq;
using System.Text.Json;
using TrackYourDay.Core.ApplicationTrackers.GitLab;

namespace TrackYourDay.Tests.ApplicationTrackers.GitLab
{
    [Trait("Category", "Unit")]
    public class GitLabActivityServiceTests
    {
        private readonly Mock<IGitLabRestApiClient> gitLabApiClient;
        private readonly GitLabActivityService gitLabActivityService;

        public GitLabActivityServiceTests()
        {
            this.gitLabApiClient = new Mock<IGitLabRestApiClient>();
            this.gitLabActivityService = new GitLabActivityService(this.gitLabApiClient.Object);
        }

        [Fact]
        public void GivenReceivedGitLabEventWithActionTypeOpenedAndTargetTypeMergeRequest_WhenGettingActivity_ThenReturnedActivityShouldDescribeOpenedMergeRequest()
        {
            // Given
            var gitLabEvent = JsonSerializer.Deserialize<GitLabEvent>(this.GetResponseFor_OpenedMergeRequest());
            this.gitLabApiClient.Setup(x => x.GetUserEvents(It.IsAny<GitLabUserId>(), It.IsAny<DateOnly>()))
                .Returns(new List<GitLabEvent> { gitLabEvent });

            // When
            var activities = this.gitLabActivityService.GetTodayActivities();

            // Then
            activities.Count.Should().Be(1);
            activities.First().OccuranceDate.Should().Be(gitLabEvent.CreatedAt.DateTime);
            activities.First().Description.Should().Be("Merge Request Opened with Title: Merge request from branch with 2 commits to master with squashing.");
        }

        [Fact]
        public void GivenReceivedGitLabEventWithActionTypePushedToAndWithoutTargetType_WhenGettingActivity_ThenReturnedActivityShouldDescribePushedCommits()
        {
            // Given
            var gitLabEvent = JsonSerializer.Deserialize<GitLabEvent>(this.GetResponseFor_PushedTwoCommits());
            this.gitLabApiClient.Setup(x => x.GetUserEvents(It.IsAny<GitLabUserId>(), It.IsAny<DateOnly>()))
                .Returns(new List<GitLabEvent> { gitLabEvent });

            throw new Exception("Continue work here");

            //TODO: mock here repository from gitlab
            //TODO: mock here branch from gitlab
            //TODO: mock here commits from gitlab

            // When
            var activities = this.gitLabActivityService.GetTodayActivities();

            // Then
            activities.Count.Should().Be(2);
            activities[0].OccuranceDate.Should().Be(DateTime.Parse("16 March 2025 at 22:06:53 CET"));
            activities[0].Description.Should().Be("Commit done to Repository: REPOSITORY_NAME, to branch: master, with Title: Merge request from branch with 2 commits to master with squashing");
            activities[1].OccuranceDate.Should().Be(DateTime.Parse("16 March 2025 at 22:06:53 CET"));
            activities[1].Description.Should().Be("Commit done to Repository: REPOSITORY_NAME, to branch: master, with Title: Merge branch 'BranchPushedOnCreation' into 'master'");
        }

        // Below are just raw responses from gitlab, not to be dependent on gitlab api but just to instantiate objects as they were real
        private string GetResponseForDeleted()
        {
            return @"
            {
                ""id"": 4105331566,
                ""project_id"": 24674429,
                ""action_name"": ""deleted"",
                ""target_id"": null,
                ""target_iid"": null,
                ""target_type"": null,
                ""author_id"": 8272154,
                ""target_title"": null,
                ""created_at"": ""2025-03-16T21:06:55.130Z"",
                ""author"": {
                    ""id"": 8272154,
                    ""username"": ""ss.skuty"",
                    ""name"": ""Adam Kuba"",
                    ""state"": ""active"",
                    ""locked"": false,
                    ""avatar_url"": ""https://secure.gravatar.com/avatar/3822593552766ab8aa67b5d48388e42ce6b992558760418a7325084dd3b6013d?s=80\u0026d=identicon"",
                    ""web_url"": ""https://gitlab.com/ss.skuty""
                },
                ""imported"": false,
                ""imported_from"": ""none"",
                ""push_data"": {
                    ""commit_count"": 0,
                    ""action"": ""removed"",
                    ""ref_type"": ""branch"",
                    ""commit_from"": ""f40b6e4ed92c3cea5ecb893d7b138be4bf422e93"",
                    ""commit_to"": null,
                    ""ref"": ""BranchPushedOnCreation"",
                    ""commit_title"": null,
                    ""ref_count"": null
                },
                ""author_username"": ""ss.skuty""
            }";
        }

        private string GetResponseFor_PushedTwoCommits()
        {
            return @"
            {
                ""id"": 4105331560,
                ""project_id"": 24674429,
                ""action_name"": ""pushed to"",
                ""target_id"": null,
                ""target_iid"": null,
                ""target_type"": null,
                ""author_id"": 8272154,
                ""target_title"": null,
                ""created_at"": ""2025-03-16T21:06:54.382Z"",
                ""author"": {
                    ""id"": 8272154,
                    ""username"": ""ss.skuty"",
                    ""name"": ""Adam Kuba"",
                    ""state"": ""active"",
                    ""locked"": false,
                    ""avatar_url"": ""https://secure.gravatar.com/avatar/3822593552766ab8aa67b5d48388e42ce6b992558760418a7325084dd3b6013d?s=80\u0026d=identicon"",
                    ""web_url"": ""https://gitlab.com/ss.skuty""
                },
                ""imported"": false,
                ""imported_from"": ""none"",
                ""push_data"": {
                    ""commit_count"": 2,
                    ""action"": ""pushed"",
                    ""ref_type"": ""branch"",
                    ""commit_from"": ""e25ac8c9903a185536d865ef7084f5e4b2f03a9e"",
                    ""commit_to"": ""d4727b4c86ebee7dbf290ec55fac5490ca1b8bc4"",
                    ""ref"": ""master"",
                    ""commit_title"": ""Merge branch 'BranchPushedOnCreation' into 'master'"",
                    ""ref_count"": null
                },
                ""author_username"": ""ss.skuty""
            }";
        }

        private string GetResponseForAccepted()
        {
            return @"
            {
                ""id"": 4105331556,
                ""project_id"": 24674429,
                ""action_name"": ""accepted"",
                ""target_id"": 369438332,
                ""target_iid"": 31,
                ""target_type"": ""MergeRequest"",
                ""author_id"": 8272154,
                ""target_title"": ""Merge request from branch with 2 commits to master with squashing"",
                ""created_at"": ""2025-03-16T21:06:54.223Z"",
                ""author"": {
                    ""id"": 8272154,
                    ""username"": ""ss.skuty"",
                    ""name"": ""Adam Kuba"",
                    ""state"": ""active"",
                    ""locked"": false,
                    ""avatar_url"": ""https://secure.gravatar.com/avatar/3822593552766ab8aa67b5d48388e42ce6b992558760418a7325084dd3b6013d?s=80\u0026d=identicon"",
                    ""web_url"": ""https://gitlab.com/ss.skuty""
                },
                ""imported"": false,
                ""imported_from"": ""none"",
                ""author_username"": ""ss.skuty""
            }";
        }

        private string GetResponseFor_OpenedMergeRequest()
        {
            return @"
            {
                ""id"": 4105331441,
                ""project_id"": 24674429,
                ""action_name"": ""opened"",
                ""target_id"": 369438332,
                ""target_iid"": 31,
                ""target_type"": ""MergeRequest"",
                ""author_id"": 8272154,
                ""target_title"": ""Merge request from branch with 2 commits to master with squashing"",
                ""created_at"": ""2025-03-16T21:06:44.829Z"",
                ""author"": {
                    ""id"": 8272154,
                    ""username"": ""ss.skuty"",
                    ""name"": ""Adam Kuba"",
                    ""state"": ""active"",
                    ""locked"": false,
                    ""avatar_url"": ""https://secure.gravatar.com/avatar/3822593552766ab8aa67b5d48388e42ce6b992558760418a7325084dd3b6013d?s=80\u0026d=identicon"",
                    ""web_url"": ""https://gitlab.com/ss.skuty""
                },
                ""imported"": false,
                ""imported_from"": ""none"",
                ""author_username"": ""ss.skuty""
            }";
        }

        private string GetResponseForPushedToAgain()
        {
            return @"
            {
                ""id"": 4105330432,
                ""project_id"": 24674429,
                ""action_name"": ""pushed to"",
                ""target_id"": null,
                ""target_iid"": null,
                ""target_type"": null,
                ""author_id"": 8272154,
                ""target_title"": null,
                ""created_at"": ""2025-03-16T21:05:22.699Z"",
                ""author"": {
                    ""id"": 8272154,
                    ""username"": ""ss.skuty"",
                    ""name"": ""Adam Kuba"",
                    ""state"": ""active"",
                    ""locked"": false,
                    ""avatar_url"": ""https://secure.gravatar.com/avatar/3822593552766ab8aa67b5d48388e42ce6b992558760418a7325084dd3b6013d?s=80\u0026d=identicon"",
                    ""web_url"": ""https://gitlab.com/ss.skuty""
                },
                ""imported"": false,
                ""imported_from"": ""none"",
                ""push_data"": {
                    ""commit_count"": 2,
                    ""action"": ""pushed"",
                    ""ref_type"": ""branch"",
                    ""commit_from"": ""e25ac8c9903a185536d865ef7084f5e4b2f03a9e"",
                    ""commit_to"": ""f40b6e4ed92c3cea5ecb893d7b138be4bf422e93"",
                    ""ref"": ""BranchPushedOnCreation"",
                    ""commit_title"": ""Second commit to branch with push but without Merge Request"",
                    ""ref_count"": null
                },
                ""author_username"": ""ss.skuty""
            }";
        }

        private string GetResponseForPushedNew()
        {
            return @"
            {
                ""id"": 4105329924,
                ""project_id"": 24674429,
                ""action_name"": ""pushed new"",
                ""target_id"": null,
                ""target_iid"": null,
                ""target_type"": null,
                ""author_id"": 8272154,
                ""target_title"": null,
                ""created_at"": ""2025-03-16T21:04:38.957Z"",
                ""author"": {
                    ""id"": 8272154,
                    ""username"": ""ss.skuty"",
                    ""name"": ""Adam Kuba"",
                    ""state"": ""active"",
                    ""locked"": false,
                    ""avatar_url"": ""https://secure.gravatar.com/avatar/3822593552766ab8aa67b5d48388e42ce6b992558760418a7325084dd3b6013d?s=80\u0026d=identicon"",
                    ""web_url"": ""https://gitlab.com/ss.skuty""
                },
                ""imported"": false,
                ""imported_from"": ""none"",
                ""push_data"": {
                    ""commit_count"": 0,
                    ""action"": ""created"",
                    ""ref_type"": ""branch"",
                    ""commit_from"": null,
                    ""commit_to"": ""e25ac8c9903a185536d865ef7084f5e4b2f03a9e"",
                    ""ref"": ""BranchPushedOnCreation"",
                    ""commit_title"": null,
                    ""ref_count"": null
                },
                ""author_username"": ""ss.skuty""
            }";
        }

        private string GetResponseForPushedToThird()
        {
            return @"
            {
                ""id"": 4105329065,
                ""project_id"": 24674429,
                ""action_name"": ""pushed to"",
                ""target_id"": null,
                ""target_iid"": null,
                ""target_type"": null,
                ""author_id"": 8272154,
                ""target_title"": null,
                ""created_at"": ""2025-03-16T21:03:54.666Z"",
                ""author"": {
                    ""id"": 8272154,
                    ""username"": ""ss.skuty"",
                    ""name"": ""Adam Kuba"",
                    ""state"": ""active"",
                    ""locked"": false,
                    ""avatar_url"": ""https://secure.gravatar.com/avatar/3822593552766ab8aa67b5d48388e42ce6b992558760418a7325084dd3b6013d?s=80\u0026d=identicon"",
                    ""web_url"": ""https://gitlab.com/ss.skuty""
                },
                ""imported"": false,
                ""imported_from"": ""none"",
                ""push_data"": {
                    ""commit_count"": 3,
                    ""action"": ""pushed"",
                    ""ref_type"": ""branch"",
                    ""commit_from"": ""417329484150fb012482d9a873bf2c68cd635a41"",
                    ""commit_to"": ""e25ac8c9903a185536d865ef7084f5e4b2f03a9e"",
                    ""ref"": ""master"",
                    ""commit_title"": ""Third commit with push directly to master"",
                    ""ref_count"": null
                },
                ""author_username"": ""ss.skuty""
            }";
        }

        private string GetResponseForPushedToFourth()
        {
            return @"
            {
                ""id"": 4105328127,
                ""project_id"": 24674429,
                ""action_name"": ""pushed to"",
                ""target_id"": null,
                ""target_iid"": null,
                ""target_type"": null,
                ""author_id"": 8272154,
                ""target_title"": null,
                ""created_at"": ""2025-03-16T21:02:51.676Z"",
                ""author"": {
                    ""id"": 8272154,
                    ""username"": ""ss.skuty"",
                    ""name"": ""Adam Kuba"",
                    ""state"": ""active"",
                    ""locked"": false,
                    ""avatar_url"": ""https://secure.gravatar.com/avatar/3822593552766ab8aa67b5d48388e42ce6b992558760418a7325084dd3b6013d?s=80\u0026d=identicon"",
                    ""web_url"": ""https://gitlab.com/ss.skuty""
                },
                ""imported"": false,
                ""imported_from"": ""none"",
                ""push_data"": {
                    ""commit_count"": 1,
                    ""action"": ""pushed"",
                    ""ref_type"": ""branch"",
                    ""commit_from"": ""27329d3afac51fbf2762428e12f2635d1137c549"",
                    ""commit_to"": ""417329484150fb012482d9a873bf2c68cd635a41"",
                    ""ref"": ""master"",
                    ""commit_title"": ""Single Commit Directly to Master pushed in the same time as commited"",
                    ""ref_count"": null
                },
                ""author_username"": ""ss.skuty""
            }";
        }

        private string GetResponseForProject()
        {
            return @"{GitLabProject { Id = 24674429, Description = , DefaultBranch = master, Visibility = private, SshUrlToRepo = git@gitlab.com:ss.skuty1/test.git, HttpUrlToRepo = https://gitlab.com/ss.skuty1/test.git, WebUrl = https://gitlab.com/ss.skuty1/test, ReadmeUrl = https://gitlab.com/ss.skuty1/test/-/blob/master/README.md, TagList = System.Collections.Generic.List`1[System.String], Owner = , Name = test, NameWithNamespace = ss.skuty / test, Path = test, PathWithNamespace = ss.skuty1/test, IssuesEnabled = True, OpenIssuesCount = 29, MergeRequestsEnabled = True, JobsEnabled = False, WikiEnabled = False, SnippetsEnabled = False, ResolveOutdatedDiffDiscussions = False, ContainerRegistryEnabled = True, CreatedAt = 24.02.2021 20:07:24 +00:00, LastActivityAt = 17.03.2025 21:48:33 +00:00, CreatorId = 8272154, ImportStatus = finished, Archived = False, AvatarUrl = , SharedRunnersEnabled = True, ForksCount = 0, StarCount = 0, RunnersToken =  }}";
        }
        private string GetResponseWith2CommitsRelatedToMergeRequest()
        {
            var firstCommit = @"{GitLabCommit { Id = d4727b4c86ebee7dbf290ec55fac5490ca1b8bc4, ShortId = d4727b4c, Title = Merge branch 'BranchPushedOnCreation' into 'master', AuthorName = Adam Kuba, AuthorEmail = ss.skuty@gmail.com, AuthoredDate = 16.03.2025 21:06:53 +00:00, CommitterName = Adam Kuba, CommitterEmail = ss.skuty@gmail.com, CommittedDate = 16.03.2025 21:06:53 +00:00, CreatedAt = 16.03.2025 21:06:53 +00:00, Message = Merge branch 'BranchPushedOnCreation' into 'master'

Merge request from branch with 2 commits to master with squashing

See merge request ss.skuty1/test!31, ParentIds = System.Collections.Generic.List`1[System.String], WebUrl = https://gitlab.com/ss.skuty1/test/-/commit/d4727b4c86ebee7dbf290ec55fac5490ca1b8bc4 }}";

            var secondCommit = @"{GitLabCommit { Id = 1e217f79538e47e5c15606f641027e827b6a49fc, ShortId = 1e217f79, Title = Merge request from branch with 2 commits to master with squashing, AuthorName = Adam Kuba, AuthorEmail = ss.skuty@gmail.com, AuthoredDate = 16.03.2025 21:06:53 +00:00, CommitterName = Adam Kuba, CommitterEmail = ss.skuty@gmail.com, CommittedDate = 16.03.2025 21:06:53 +00:00, CreatedAt = 16.03.2025 21:06:53 +00:00, Message = Merge request from branch with 2 commits to master with squashing
, ParentIds = System.Collections.Generic.List`1[System.String], WebUrl = https://gitlab.com/ss.skuty1/test/-/commit/1e217f79538e47e5c15606f641027e827b6a49fc }}";
            return @"";
        }
    }
}
