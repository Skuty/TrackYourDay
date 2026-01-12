using TrackYourDay.Web.Events;
using TrackYourDay.Web.Services;

namespace TrackYourDay.Web.ServiceRegistration
{
    public static class ServiceCollections
    {
        public static IServiceCollection AddEventHandlingForBlazorUIComponents(this IServiceCollection services)
        {
            services.AddSingleton<EventWrapperForComponents>();
            services.AddSingleton<ActiveMeetingConfirmationsService>();
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<EventWrapperForComponents>());

            return services;
        }
    }
}
