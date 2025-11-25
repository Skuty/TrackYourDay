using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.UserTasks;
using TrackYourDay.Core.Insights.Analytics;
using TrackYourDay.Core.Insights.Workdays;
using TrackYourDay.Core.Notifications;
using TrackYourDay.Core.Settings;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Core.SystemTrackers.ActivityRecognizing;
using TrackYourDay.Core.Versioning;
using TrackYourDay.Core.ApplicationTrackers.Jira;

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
                var activityRepository = container.GetRequiredService<IActivityRepository>();

                return new ActivityTracker(
                    clock,
                    publisher,
                    focusedWindowRecognizingStategy,
                    mousePositionRecognizingStrategy,
                    lastInputRecognizingStrategy,
                    logger,
                    activityRepository);
            });

            services.AddSingleton<MsTeamsMeetingTracker>(container =>
            {
                var clock = container.GetRequiredService<IClock>();
                var publisher = container.GetRequiredService<IPublisher>();
                var loggerForStrategy = container.GetRequiredService<ILogger<ProcessBasedMeetingRecognizingStrategy>>();
                var meetingDiscoveryStrategy = new ProcessBasedMeetingRecognizingStrategy(loggerForStrategy, new WindowsProcessService());
                var loggerForMs = container.GetRequiredService<ILogger<MsTeamsMeetingTracker>>();
                var meetingRepository = container.GetRequiredService<IMeetingRepository>();
                return new MsTeamsMeetingTracker(clock, publisher, meetingDiscoveryStrategy, loggerForMs, meetingRepository);
            });

            services.AddSingleton<BreakTracker>(serviceCollection => 
            {
                var breaksSettingsService = serviceCollection.GetRequiredService<IBreaksSettingsService>();
                var breaksSettings = breaksSettingsService.GetSettings();
                var breakRepository = serviceCollection.GetRequiredService<IBreakRepository>();
                
                return new BreakTracker(
                    serviceCollection.GetRequiredService<IPublisher>(),
                    serviceCollection.GetRequiredService<IClock>(),
                    breaksSettings.TimeOfNoActivityToStartBreak,
                    serviceCollection.GetRequiredService<ILogger<BreakTracker>>(),
                    breakRepository);
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
                return GitLabRestApiClientFactory.Create(gitLabSettingsService.GetSettings());
            });

            services.AddSingleton<GitLabActivityService>();
            services.AddSingleton<GitLabTracker>();

            services.AddSingleton<IJiraRestApiClient>(serviceCollection =>
            {
                var jiraSettingsService = serviceCollection.GetRequiredService<IJiraSettingsService>();
                return JiraRestApiClientFactory.Create(jiraSettingsService.GetSettings());
            });

            services.AddSingleton<IJiraActivityService, JiraActivityService>();
            services.AddSingleton<JiraTracker>();

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
            // Register unified repository for each type with adapters
            services.AddSingleton<IActivityRepository>(sp => 
                new SystemTrackers.ActivityRepositoryAdapter(
                    new TrackYourDay.Core.Persistence.SqliteHistoricalDataRepository<SystemTrackers.EndedActivity>()));
            services.AddSingleton<IBreakRepository>(sp => 
                new ApplicationTrackers.Breaks.BreakRepositoryAdapter(
                    new TrackYourDay.Core.Persistence.SqliteHistoricalDataRepository<ApplicationTrackers.Breaks.EndedBreak>()));
            services.AddSingleton<IMeetingRepository>(sp => 
                new ApplicationTrackers.MsTeams.MeetingRepositoryAdapter(
                    new TrackYourDay.Core.Persistence.SqliteHistoricalDataRepository<ApplicationTrackers.MsTeams.EndedMeeting>()));
            
            services.AddSingleton<TrackYourDay.Core.Persistence.HistoricalDataService>(sp =>
            {
                var historicalDataService = new TrackYourDay.Core.Persistence.HistoricalDataService(
                    sp.GetRequiredService<IClock>(),
                    sp.GetRequiredService<ActivityTracker>(),
                    sp.GetRequiredService<BreakTracker>(),
                    sp.GetRequiredService<MsTeamsMeetingTracker>());

                // Register repositories only (trackers are now injected via constructor)
                historicalDataService.RegisterRepository(sp.GetRequiredService<IActivityRepository>());
                historicalDataService.RegisterRepository(sp.GetRequiredService<IBreakRepository>());
                historicalDataService.RegisterRepository(sp.GetRequiredService<IMeetingRepository>());

                return historicalDataService;
            });

            return services;
        }
    }
}
