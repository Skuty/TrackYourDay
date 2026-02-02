using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.Persistence;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.RuleEngine;
using TrackYourDay.Core.ApplicationTrackers.UserTasks;
using TrackYourDay.Core.ApplicationTrackers.Persistence;
using TrackYourDay.Core.Insights.Analytics;
using TrackYourDay.Core.Insights.Workdays;
using TrackYourDay.Core.Notifications;
using TrackYourDay.Core.Persistence;
using TrackYourDay.Core.Persistence.EventHandlers;
using TrackYourDay.Core.Settings;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Core.SystemTrackers.ActivityRecognizing;
using TrackYourDay.Core.Versioning;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.LlmPrompts;

namespace TrackYourDay.Core.ServiceRegistration
{
    // Narrow this class to conain only core service registrations
    // Now those are udplicated between here and maui project reigstraiton
    // Then register THIS service in WEB project ase core and required
    public static class ServiceCollections
    {
        public static IServiceCollection AddTrackers(this IServiceCollection services)
        {
            services.AddScoped<ISystemStateRecognizingStrategy, FocusedWindowRecognizingStategy>();
            // Refactor to avoid this in future
            services.AddSingleton<ActivityTracker>(container =>
            {
                var clock = container.GetRequiredService<IClock>();
                var publisher = container.GetRequiredService<IPublisher>();
                var focusedWindowRecognizingStategy = new FocusedWindowRecognizingStategy();
                var mousePositionRecognizingStrategy = new MousePositionRecognizingStrategy();
                var lastInputRecognizingStrategy = new LastInputRecognizingStrategy();
                var logger = container.GetRequiredService<ILogger<ActivityTracker>>();

                return new ActivityTracker(
                    clock,
                    publisher,
                    focusedWindowRecognizingStategy,
                    mousePositionRecognizingStrategy,
                    lastInputRecognizingStrategy,
                    logger);
            });

            services.AddSingleton<IMeetingRuleRepository, MeetingRuleRepository>();
            services.AddSingleton<IProcessService, WindowsProcessService>();
            services.AddSingleton<MsTeamsMeetingTracker>();
            services.AddScoped<IMeetingDiscoveryStrategy, ConfigurableMeetingDiscoveryStrategy>();
            services.AddScoped<IMeetingRuleEngine, MeetingRuleEngine>();

            services.AddSingleton<BreakTracker>(serviceCollection => 
            {
                var breaksSettingsService = serviceCollection.GetRequiredService<IBreaksSettingsService>();
                var breaksSettings = breaksSettingsService.GetSettings();
                
                return new BreakTracker(
                    serviceCollection.GetRequiredService<IPublisher>(),
                    serviceCollection.GetRequiredService<IClock>(),
                    breaksSettings.TimeOfNoActivityToStartBreak,
                    serviceCollection.GetRequiredService<ILogger<BreakTracker>>());
            });

            services.AddSingleton<MLNetSummaryStrategy>(serviceProvider => 
                new MLNetSummaryStrategy(
                    serviceProvider.GetRequiredService<IClock>(),
                    serviceProvider.GetRequiredService<ILogger<MLNetSummaryStrategy>>()
                )
            );

            services.AddSingleton<ISummaryStrategy, MLNetSummaryStrategy>(serviceProvider => 
                serviceProvider.GetRequiredService<MLNetSummaryStrategy>()
            );

            services.AddSingleton<ActivitiesAnalyser>(serviceProvider => 
                new ActivitiesAnalyser(
                    serviceProvider.GetRequiredService<IClock>(),
                    serviceProvider.GetRequiredService<IPublisher>(),
                    serviceProvider.GetRequiredService<ILogger<ActivitiesAnalyser>>(),
                    serviceProvider.GetRequiredService<ISummaryStrategy>()
                )
            );

            services.AddSingleton<UserTaskService>();

            services.AddSingleton<IGitLabRestApiClient>(serviceCollection =>
            {
                var gitLabSettingsService = serviceCollection.GetRequiredService<IGitLabSettingsService>();
                var httpClientFactory = serviceCollection.GetRequiredService<IHttpClientFactory>();
                return GitLabRestApiClientFactory.Create(gitLabSettingsService.GetSettings(), httpClientFactory);
            });

            services.AddSingleton<IGitLabActivityService, GitLabActivityService>();
            services.AddSingleton<IGitLabStateRepository, GitLabStateRepository>();
            services.AddSingleton<IGitLabStateService, GitLabStateService>();
            
            services.AddSingleton<GitLabTracker>(serviceProvider =>
                new GitLabTracker(
                    serviceProvider.GetRequiredService<IGitLabActivityService>(),
                    serviceProvider.GetRequiredService<IHistoricalDataRepository<GitLabActivity>>(),
                    serviceProvider.GetRequiredService<IGitLabSettingsService>(),
                    serviceProvider.GetRequiredService<IPublisher>(),
                    serviceProvider.GetRequiredService<ILogger<GitLabTracker>>()
                )
            );

            services.AddSingleton<IJiraRestApiClient>(serviceCollection =>
            {
                var jiraSettingsService = serviceCollection.GetRequiredService<IJiraSettingsService>();
                var httpClientFactory = serviceCollection.GetRequiredService<IHttpClientFactory>();
                var logger = serviceCollection.GetRequiredService<ILogger<JiraRestApiClient>>();
                return JiraRestApiClientFactory.Create(jiraSettingsService.GetSettings(), httpClientFactory, logger);
            });

            services.AddSingleton<IJiraActivityService, JiraActivityService>();
            services.AddSingleton<IJiraCurrentStateService, JiraCurrentStateService>();
            services.AddSingleton<JiraTracker>(serviceProvider =>
                new JiraTracker(
                    serviceProvider.GetRequiredService<IJiraActivityService>(),
                    serviceProvider.GetRequiredService<IHistoricalDataRepository<JiraActivity>>(),
                    serviceProvider.GetRequiredService<IJiraSettingsService>(),
                    serviceProvider.GetRequiredService<ILogger<JiraTracker>>()
                )
            );

            services.AddSingleton<JiraKeySummaryStrategy>(serviceProvider =>
                new JiraKeySummaryStrategy(
                    serviceProvider.GetRequiredService<ILogger<JiraKeySummaryStrategy>>()
                )
            );

            services.AddSingleton<TimeBasedSummaryStrategy>(serviceProvider =>
                new TimeBasedSummaryStrategy(
                    serviceProvider.GetRequiredService<ILogger<TimeBasedSummaryStrategy>>()
                )
            );

            services.AddSingleton<DurationBasedSummaryStrategy>(serviceProvider =>
                new DurationBasedSummaryStrategy(
                    serviceProvider.GetRequiredService<ILogger<DurationBasedSummaryStrategy>>()
                )
            );

            services.AddSingleton<ContextBasedSummaryStrategy>(serviceProvider =>
                new ContextBasedSummaryStrategy(
                    serviceProvider.GetRequiredService<ILogger<ContextBasedSummaryStrategy>>()
                )
            );

            services.AddSingleton<JiraEnrichedSummaryStrategy>(serviceProvider =>
                new JiraEnrichedSummaryStrategy(
                    serviceProvider.GetRequiredService<JiraTracker>(),
                    serviceProvider.GetRequiredService<ILogger<JiraEnrichedSummaryStrategy>>()
                )
            );

            services.AddSingleton<HybridContextualSummaryStrategy>(serviceProvider =>
                new HybridContextualSummaryStrategy(
                    serviceProvider.GetRequiredService<ILogger<HybridContextualSummaryStrategy>>()
                )
            );

            services.AddSingleton<ActivityNameSummaryStrategy>(serviceProvider =>
                new ActivityNameSummaryStrategy(
                    serviceProvider.GetRequiredService<ILogger<ActivityNameSummaryStrategy>>()
                )
            );

            return services;
        }

        public static IServiceCollection AddCoreNotifications(this IServiceCollection services)
        {
            services.AddSingleton<WorkdayReadModelRepository>();
            services.AddSingleton<NotificationRepository>();
            services.AddSingleton<NotificationService>();

            return services;
        }

        public static IServiceCollection AddSettings(this IServiceCollection services)
        {
            services.AddSingleton<IEncryptionService, EncryptionService>();
            services.AddSingleton<IClock, Clock>();
            services.AddSingleton<VersioningSystemFacade, VersioningSystemFacade>();
            
            // Generic settings infrastructure
            services.AddSingleton<IGenericSettingsRepository, SqliteGenericSettingsRepository>();
            services.AddSingleton<IGenericSettingsService, GenericSettingsService>();
            
            // Specific settings services
            services.AddSingleton<IGitLabSettingsService, GitLabSettingsService>();
            services.AddSingleton<IJiraSettingsService, JiraSettingsService>();
            services.AddSingleton<IBreaksSettingsService, BreaksSettingsService>();
            services.AddSingleton<IActivitiesSettingsService, ActivitiesSettingsService>();
            services.AddSingleton<IWorkdaySettingsService, WorkdaySettingsService>();

            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            // Register generic repositories with tracker integration
            services.AddSingleton<IHistoricalDataRepository<EndedActivity>>(sp => 
                new GenericDataRepository<EndedActivity>(
                    sp.GetRequiredService<IClock>(),
                    sp.GetRequiredService<ISqliteConnectionFactory>(),
                    () => sp.GetRequiredService<ActivityTracker>().GetEndedActivities()));
            
            services.AddSingleton<IHistoricalDataRepository<EndedBreak>>(sp => 
                new GenericDataRepository<EndedBreak>(
                    sp.GetRequiredService<IClock>(),
                    sp.GetRequiredService<ISqliteConnectionFactory>(),
                    () => sp.GetRequiredService<BreakTracker>().GetEndedBreaks()));
            
            services.AddSingleton<IHistoricalDataRepository<EndedMeeting>>(sp => 
                new GenericDataRepository<EndedMeeting>(
                    sp.GetRequiredService<IClock>(),
                    sp.GetRequiredService<ISqliteConnectionFactory>(),
                    null));

            services.AddSingleton<IHistoricalDataRepository<GitLabActivity>>(sp => 
                new GenericDataRepository<GitLabActivity>(
                    sp.GetRequiredService<IClock>(),
                    sp.GetRequiredService<ISqliteConnectionFactory>(),
                    null));

            services.AddSingleton<IHistoricalDataRepository<JiraActivity>>(sp => 
                new GenericDataRepository<JiraActivity>(
                    sp.GetRequiredService<IClock>(),
                    sp.GetRequiredService<ISqliteConnectionFactory>(),
                    null));

            return services;
        }

        public static IServiceCollection AddLlmPromptServices(this IServiceCollection services)
        {
            services.AddSingleton<ILlmPromptService, LlmPromptService>();
            services.AddSingleton<ITemplateManagementService, TemplateManagementService>();

            return services;
        }
    }
}
