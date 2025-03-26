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
            var gitlabUser = JsonSerializer.Deserialize<GitLabUser>(this.GetResponseFor_GetCurrentUser());
            this.gitLabApiClient.Setup(x => x.GetCurrentUser()).Returns(gitlabUser);
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
            activities.First().Description.Should().Be("Merge Request Opened with Title: Merge request from branch with 2 commits to master with squashing");
        }

        [Fact]
        public void GivenReceivedGitLabEventWithActionTypePushedToAndWithoutTargetType_WhenGettingActivity_ThenReturnedActivityShouldDescribePushedCommits()
        {
            // Given
            var gitLabEvent = JsonSerializer.Deserialize<GitLabEvent>(this.GetResponseFor_PushedTwoCommits());
            this.gitLabApiClient.Setup(x => x.GetUserEvents(It.IsAny<GitLabUserId>(), It.IsAny<DateOnly>()))
                .Returns(new List<GitLabEvent> { gitLabEvent });

            var gitLabProject = JsonSerializer.Deserialize<GitLabProject>(this.GetResponseForProject());
            this.gitLabApiClient.Setup(gitLabApiClient => gitLabApiClient.GetProject(It.IsAny<GitLabProjectId>()))
                .Returns(gitLabProject);

            var gitLabCommits = JsonSerializer.Deserialize<List<GitLabCommit>>(this.GetResponseWith2CommitsRelatedToMergeRequest());
            this.gitLabApiClient.Setup(x => x.GetCommits(It.IsAny<GitLabProjectId>(), It.IsAny<GitLabRefName>(), It.IsAny<DateOnly>()))
                .Returns(gitLabCommits);

            // When
            var activities = this.gitLabActivityService.GetTodayActivities();

            // Then
            activities.Count.Should().Be(2);
            // TODO Dates probably have to be handled in more conscious way including time zones and offsets
            activities[0].OccuranceDate.Should().Be(new DateTime(2025, 03, 16, 21, 06, 53, DateTimeKind.Utc));
            activities[0].Description.Should().Be("Commit done to Repository: ss.skuty / test, to branch: master, with Title: Merge branch 'BranchPushedOnCreation' into 'master'");
            activities[1].OccuranceDate.Should().Be(new DateTime(2025, 03, 16, 21, 06, 53, DateTimeKind.Utc));
            activities[1].Description.Should().Be("Commit done to Repository: ss.skuty / test, to branch: master, with Title: Merge request from branch with 2 commits to master with squashing");
        }

        // Below are just raw responses from gitlab, not to be dependent on gitlab api but just to instantiate objects as they were real
        private string GetResponseFor_GetCurrentUser()
        {
            return @"
            {
                ""id"": 8272154,
                ""username"": ""ss.skuty"",
                ""name"": ""Adam Kuba"",
                ""state"": ""active"",
                ""locked"": false,
                ""avatar_url"": ""https://secure.gravatar.com/avatar/3822593552766ab8aa67b5d48388e42ce6b992558760418a7325084dd3b6013d?s=80\u0026d=identicon"",
                ""web_url"": ""https://gitlab.com/ss.skuty"",
                ""created_at"": ""2021-02-24T20:01:34.401Z"",
                ""bio"": """",
                ""location"": """",
                ""public_email"": """",
                ""skype"": """",
                ""linkedin"": """",
                ""twitter"": """",
                ""discord"": """",
                ""website_url"": """",
                ""organization"": """",
                ""job_title"": """",
                ""pronouns"": null,
                ""bot"": false,
                ""work_information"": null,
                ""local_time"": ""10:02 PM"",
                ""last_sign_in_at"": ""2025-03-15T20:18:32.940Z"",
                ""confirmed_at"": ""2021-02-24T20:02:41.148Z"",
                ""last_activity_on"": ""2025-03-26"",
                ""email"": ""ss.skuty@gmail.com"",
                ""theme_id"": 1,
                ""color_scheme_id"": 1,
                ""projects_limit"": 100000,
                ""current_sign_in_at"": ""2025-03-26T21:12:35.307Z"",
                ""identities"": [],
                ""can_create_group"": true,
                ""can_create_project"": true,
                ""two_factor_enabled"": false,
                ""external"": false,
                ""private_profile"": false,
                ""commit_email"": ""ss.skuty@gmail.com"",
                ""shared_runners_minutes_limit"": null,
                ""extra_shared_runners_minutes_limit"": null,
                ""scim_identities"": []
            }";
        }

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
            return @"
            {
                ""id"": 24674429,
                ""description"": """",
                ""name"": ""test"",
                ""name_with_namespace"": ""ss.skuty / test"",
                ""path"": ""test"",
                ""path_with_namespace"": ""ss.skuty1/test"",
                ""created_at"": ""2021-02-24T20:07:24.515Z"",
                ""default_branch"": ""master"",
                ""tag_list"": [],
                ""topics"": [],
                ""ssh_url_to_repo"": ""git@gitlab.com:ss.skuty1/test.git"",
                ""http_url_to_repo"": ""https://gitlab.com/ss.skuty1/test.git"",
                ""web_url"": ""https://gitlab.com/ss.skuty1/test"",
                ""readme_url"": ""https://gitlab.com/ss.skuty1/test/-/blob/master/README.md"",
                ""forks_count"": 0,
                ""avatar_url"": null,
                ""star_count"": 0,
                ""last_activity_at"": ""2025-03-17T21:48:33.777Z"",
                ""namespace"": {
                    ""id"": 11094361,
                    ""name"": ""ss.skuty"",
                    ""path"": ""ss.skuty1"",
                    ""kind"": ""group"",
                    ""full_path"": ""ss.skuty1"",
                    ""parent_id"": null,
                    ""avatar_url"": null,
                    ""web_url"": ""https://gitlab.com/groups/ss.skuty1""
                },
                ""container_registry_image_prefix"": ""registry.gitlab.com/ss.skuty1/test"",
                ""_links"": {
                    ""self"": ""https://gitlab.com/api/v4/projects/24674429"",
                    ""issues"": ""https://gitlab.com/api/v4/projects/24674429/issues"",
                    ""merge_requests"": ""https://gitlab.com/api/v4/projects/24674429/merge_requests"",
                    ""repo_branches"": ""https://gitlab.com/api/v4/projects/24674429/repository/branches"",
                    ""labels"": ""https://gitlab.com/api/v4/projects/24674429/labels"",
                    ""events"": ""https://gitlab.com/api/v4/projects/24674429/events"",
                    ""members"": ""https://gitlab.com/api/v4/projects/24674429/members"",
                    ""cluster_agents"": ""https://gitlab.com/api/v4/projects/24674429/cluster_agents""
                },
                ""packages_enabled"": true,
                ""empty_repo"": false,
                ""archived"": false,
                ""visibility"": ""private"",
                ""resolve_outdated_diff_discussions"": false,
                ""container_expiration_policy"": {
                    ""cadence"": ""1d"",
                    ""enabled"": false,
                    ""keep_n"": 10,
                    ""older_than"": ""90d"",
                    ""name_regex"": "". *"",
                    ""name_regex_keep"": null,
                    ""next_run_at"": ""2021-02-25T20:08:25.518Z""
                },
                ""repository_object_format"": ""sha1"",
                ""issues_enabled"": true,
                ""merge_requests_enabled"": true,
                ""wiki_enabled"": false,
                ""jobs_enabled"": false,
                ""snippets_enabled"": false,
                ""container_registry_enabled"": true,
                ""service_desk_enabled"": false,
                ""service_desk_address"": ""contact-project+ss-skuty1-test-24674429-issue-@incoming.gitlab.com"",
                ""can_create_merge_request_in"": true,
                ""issues_access_level"": ""enabled"",
                ""repository_access_level"": ""enabled"",
                ""merge_requests_access_level"": ""enabled"",
                ""forking_access_level"": ""enabled"",
                ""wiki_access_level"": ""disabled"",
                ""builds_access_level"": ""disabled"",
                ""snippets_access_level"": ""disabled"",
                ""pages_access_level"": ""enabled"",
                ""analytics_access_level"": ""disabled"",
                ""container_registry_access_level"": ""enabled"",
                ""security_and_compliance_access_level"": ""private"",
                ""releases_access_level"": ""enabled"",
                ""environments_access_level"": ""disabled"",
                ""feature_flags_access_level"": ""disabled"",
                ""infrastructure_access_level"": ""disabled"",
                ""monitor_access_level"": ""disabled"",
                ""model_experiments_access_level"": ""enabled"",
                ""model_registry_access_level"": ""enabled"",
                ""emails_disabled"": false,
                ""emails_enabled"": true,
                ""shared_runners_enabled"": true,
                ""lfs_enabled"": true,
                ""creator_id"": 8272154,
                ""import_url"": null,
                ""import_type"": ""gitlab_project"",
                ""import_status"": ""finished"",
                ""import_error"": null,
                ""open_issues_count"": 29,
                ""description_html"": """",
                ""updated_at"": ""2025-03-17T21:48:33.777Z"",
                ""ci_default_git_depth"": 50,
                ""ci_delete_pipelines_in_seconds"": null,
                ""ci_forward_deployment_enabled"": true,
                ""ci_forward_deployment_rollback_allowed"": true,
                ""ci_job_token_scope_enabled"": false,
                ""ci_separated_caches"": true,
                ""ci_allow_fork_pipelines_to_run_in_parent_project"": true,
                ""ci_id_token_sub_claim_components"": [
                    ""project_path"",
                    ""ref_type"",
                    ""ref""
                ],
                ""build_git_strategy"": ""fetch"",
                ""keep_latest_artifact"": true,
                ""restrict_user_defined_variables"": true,
                ""ci_pipeline_variables_minimum_override_role"": ""developer"",
                ""runners_token"": null,
                ""runner_token_expiration_interval"": null,
                ""group_runners_enabled"": true,
                ""auto_cancel_pending_pipelines"": ""enabled"",
                ""build_timeout"": 3600,
                ""auto_devops_enabled"": false,
                ""auto_devops_deploy_strategy"": ""continuous"",
                ""ci_push_repository_for_job_token_allowed"": false,
                ""ci_config_path"": null,
                ""public_jobs"": true,
                ""shared_with_groups"": [],
                ""only_allow_merge_if_pipeline_succeeds"": false,
                ""allow_merge_on_skipped_pipeline"": null,
                ""request_access_enabled"": true,
                ""only_allow_merge_if_all_discussions_are_resolved"": false,
                ""remove_source_branch_after_merge"": true,
                ""printing_merge_request_link_enabled"": true,
                ""merge_method"": ""merge"",
                ""squash_option"": ""default_off"",
                ""enforce_auth_checks_on_uploads"": true,
                ""suggestion_commit_message"": null,
                ""merge_commit_template"": null,
                ""squash_commit_template"": null,
                ""issue_branch_template"": null,
                ""warn_about_potentially_unwanted_characters"": true,
                ""autoclose_referenced_issues"": true,
                ""max_artifacts_size"": null,
                ""external_authorization_classification_label"": """",
                ""requirements_enabled"": false,
                ""requirements_access_level"": ""enabled"",
                ""security_and_compliance_enabled"": true,
                ""compliance_frameworks"": [],
                ""permissions"": {
                    ""project_access"": {
                        ""access_level"": 40,
                        ""notification_level"": 3
                    },
                    ""group_access"": {
                        ""access_level"": 50,
                        ""notification_level"": 3
                    }
                }
            }";
        }
        private string GetResponseWith2CommitsRelatedToMergeRequest()
        {
            var allCommits = @"
            [
                {
                    ""id"": ""d4727b4c86ebee7dbf290ec55fac5490ca1b8bc4"",
                    ""short_id"": ""d4727b4c"",
                    ""created_at"": ""2025-03-16T21:06:53.000+00:00"",
                    ""parent_ids"": [
                        ""e25ac8c9903a185536d865ef7084f5e4b2f03a9e"",
                        ""1e217f79538e47e5c15606f641027e827b6a49fc""
                    ],
                    ""title"": ""Merge branch 'BranchPushedOnCreation' into 'master'"",
                    ""message"": ""Merge branch 'BranchPushedOnCreation' into 'master'\n\nMerge request from branch with 2 commits to master with squashing\n\nSee merge request ss.skuty1/test!31"",
                    ""author_name"": ""Adam Kuba"",
                    ""author_email"": ""ss.skuty@gmail.com"",
                    ""authored_date"": ""2025-03-16T21:06:53.000+00:00"",
                    ""committer_name"": ""Adam Kuba"",
                    ""committer_email"": ""ss.skuty@gmail.com"",
                    ""committed_date"": ""2025-03-16T21:06:53.000+00:00"",
                    ""trailers"": {},
                    ""extended_trailers"": {},
                    ""web_url"": ""https://gitlab.com/ss.skuty1/test/-/commit/d4727b4c86ebee7dbf290ec55fac5490ca1b8bc4""
                },
                {
                    ""id"": ""1e217f79538e47e5c15606f641027e827b6a49fc"",
                    ""short_id"": ""1e217f79"",
                    ""created_at"": ""2025-03-16T21:06:53.000+00:00"",
                    ""parent_ids"": [
                        ""e25ac8c9903a185536d865ef7084f5e4b2f03a9e""
                    ],
                    ""title"": ""Merge request from branch with 2 commits to master with squashing"",
                    ""message"": ""Merge request from branch with 2 commits to master with squashing\n"",
                    ""author_name"": ""Adam Kuba"",
                    ""author_email"": ""ss.skuty@gmail.com"",
                    ""authored_date"": ""2025-03-16T21:06:53.000+00:00"",
                    ""committer_name"": ""Adam Kuba"",
                    ""committer_email"": ""ss.skuty@gmail.com"",
                    ""committed_date"": ""2025-03-16T21:06:53.000+00:00"",
                    ""trailers"": {},
                    ""extended_trailers"": {},
                    ""web_url"": ""https://gitlab.com/ss.skuty1/test/-/commit/1e217f79538e47e5c15606f641027e827b6a49fc""
                },
                {
                    ""id"": ""e25ac8c9903a185536d865ef7084f5e4b2f03a9e"",
                    ""short_id"": ""e25ac8c9"",
                    ""created_at"": ""2025-03-16T22:03:48.000+01:00"",
                    ""parent_ids"": [
                        ""02b5f3780d1a6b8df537cba93b405ab71cb680b4""
                    ],
                    ""title"": ""Third commit with push directly to master"",
                    ""message"": ""Third commit with push directly to master\n"",
                    ""author_name"": ""Adam"",
                    ""author_email"": ""ss.skuty@gmail.com"",
                    ""authored_date"": ""2025-03-16T22:03:48.000+01:00"",
                    ""committer_name"": ""Adam"",
                    ""committer_email"": ""ss.skuty@gmail.com"",
                    ""committed_date"": ""2025-03-16T22:03:48.000+01:00"",
                    ""trailers"": {},
                    ""extended_trailers"": {},
                    ""web_url"": ""https://gitlab.com/ss.skuty1/test/-/commit/e25ac8c9903a185536d865ef7084f5e4b2f03a9e""
                },
                {
                    ""id"": ""02b5f3780d1a6b8df537cba93b405ab71cb680b4"",
                    ""short_id"": ""02b5f378"",
                    ""created_at"": ""2025-03-16T22:03:27.000+01:00"",
                    ""parent_ids"": [
                        ""b0406bec7032da5e9d9bc99097b9d4c986b3accd""
                    ],
                    ""title"": ""Second commit without push directly to master"",
                    ""message"": ""Second commit without push directly to master\n"",
                    ""author_name"": ""Adam"",
                    ""author_email"": ""ss.skuty@gmail.com"",
                    ""authored_date"": ""2025-03-16T22:03:27.000+01:00"",
                    ""committer_name"": ""Adam"",
                    ""committer_email"": ""ss.skuty@gmail.com"",
                    ""committed_date"": ""2025-03-16T22:03:27.000+01:00"",
                    ""trailers"": {},
                    ""extended_trailers"": {},
                    ""web_url"": ""https://gitlab.com/ss.skuty1/test/-/commit/02b5f3780d1a6b8df537cba93b405ab71cb680b4""
                },
                {
                    ""id"": ""b0406bec7032da5e9d9bc99097b9d4c986b3accd"",
                    ""short_id"": ""b0406bec"",
                    ""created_at"": ""2025-03-16T22:03:05.000+01:00"",
                    ""parent_ids"": [
                        ""417329484150fb012482d9a873bf2c68cd635a41""
                    ],
                    ""title"": ""First Commit without push"",
                    ""message"": ""First Commit without push\n"",
                    ""author_name"": ""Adam"",
                    ""author_email"": ""ss.skuty@gmail.com"",
                    ""authored_date"": ""2025-03-16T22:03:05.000+01:00"",
                    ""committer_name"": ""Adam"",
                    ""committer_email"": ""ss.skuty@gmail.com"",
                    ""committed_date"": ""2025-03-16T22:03:05.000+01:00"",
                    ""trailers"": {},
                    ""extended_trailers"": {},
                    ""web_url"": ""https://gitlab.com/ss.skuty1/test/-/commit/b0406bec7032da5e9d9bc99097b9d4c986b3accd""
                },
                {
                    ""id"": ""417329484150fb012482d9a873bf2c68cd635a41"",
                    ""short_id"": ""41732948"",
                    ""created_at"": ""2025-03-16T22:02:44.000+01:00"",
                    ""parent_ids"": [
                        ""27329d3afac51fbf2762428e12f2635d1137c549""
                    ],
                    ""title"": ""Single Commit Directly to Master pushed in the same time as commited"",
                    ""message"": ""Single Commit Directly to Master pushed in the same time as commited\n"",
                    ""author_name"": ""Adam"",
                    ""author_email"": ""ss.skuty@gmail.com"",
                    ""authored_date"": ""2025-03-16T22:02:44.000+01:00"",
                    ""committer_name"": ""Adam"",
                    ""committer_email"": ""ss.skuty@gmail.com"",
                    ""committed_date"": ""2025-03-16T22:02:44.000+01:00"",
                    ""trailers"": {},
                    ""extended_trailers"": {},
                    ""web_url"": ""https://gitlab.com/ss.skuty1/test/-/commit/417329484150fb012482d9a873bf2c68cd635a41""
                }
            ]";

            var firstCommit = @"
                {
                    ""id"": ""d4727b4c86ebee7dbf290ec55fac5490ca1b8bc4"",
                    ""short_id"": ""d4727b4c"",
                    ""created_at"": ""2025-03-16T21:06:53.000+00:00"",
                    ""parent_ids"": [
                        ""e25ac8c9903a185536d865ef7084f5e4b2f03a9e"",
                        ""1e217f79538e47e5c15606f641027e827b6a49fc""
                    ],
                    ""title"": ""Merge branch 'BranchPushedOnCreation' into 'master'"",
                    ""message"": ""Merge branch 'BranchPushedOnCreation' into 'master'\n\nMerge request from branch with 2 commits to master with squashing\n\nSee merge request ss.skuty1/test!31"",
                    ""author_name"": ""Adam Kuba"",
                    ""author_email"": ""ss.skuty@gmail.com"",
                    ""authored_date"": ""2025-03-16T21:06:53.000+00:00"",
                    ""committer_name"": ""Adam Kuba"",
                    ""committer_email"": ""ss.skuty@gmail.com"",
                    ""committed_date"": ""2025-03-16T21:06:53.000+00:00"",
                    ""trailers"": {},
                    ""extended_trailers"": {},
                    ""web_url"": ""https://gitlab.com/ss.skuty1/test/-/commit/d4727b4c86ebee7dbf290ec55fac5490ca1b8bc4""
                }
                ";

            var secondCommit = @"
                {
                    ""id"": ""1e217f79538e47e5c15606f641027e827b6a49fc"",
                    ""short_id"": ""1e217f79"",
                    ""created_at"": ""2025-03-16T21:06:53.000+00:00"",
                    ""parent_ids"": [
                        ""e25ac8c9903a185536d865ef7084f5e4b2f03a9e""
                    ],
                    ""title"": ""Merge request from branch with 2 commits to master with squashing"",
                    ""message"": ""Merge request from branch with 2 commits to master with squashing\n"",
                    ""author_name"": ""Adam Kuba"",
                    ""author_email"": ""ss.skuty@gmail.com"",
                    ""authored_date"": ""2025-03-16T21:06:53.000+00:00"",
                    ""committer_name"": ""Adam Kuba"",
                    ""committer_email"": ""ss.skuty@gmail.com"",
                    ""committed_date"": ""2025-03-16T21:06:53.000+00:00"",
                    ""trailers"": {},
                    ""extended_trailers"": {},
                    ""web_url"": ""https://gitlab.com/ss.skuty1/test/-/commit/1e217f79538e47e5c15606f641027e827b6a49fc""
                }";

            return $"[{firstCommit}, {secondCommit}]";
        }
    }
}
