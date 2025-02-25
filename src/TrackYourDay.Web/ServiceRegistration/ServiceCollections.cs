using TrackYourDay.Web.Events;

namespace TrackYourDay.Web.ServiceRegistration
{
    public static class ServiceCollections
    {
        public static IServiceCollection AddEventHandlingForBlazorUIComponents(this IServiceCollection services)
        {
            services.AddScoped<EventWrapperForComponents>();
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<EventWrapperForComponents>());

            return services;
        }
    }
}
