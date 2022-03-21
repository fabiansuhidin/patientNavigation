using Appointment.Consumer;
using Microsoft.Extensions.Options;
using PatientNavigation.Common.Options;
using PatientNavigation.Common.Repositories;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;
        services.Configure<MongoDatabaseSettings>(
            configuration.GetSection(nameof(MongoDatabaseSettings)));
        services.AddSingleton<IMongoDatabaseSettings>(sp =>
            sp.GetRequiredService<IOptions<MongoDatabaseSettings>>().Value);
        services.AddTransient<IAppointmentRepository, AppointmentRepository>();
        services.AddTransient<IAppointmentService, AppointmentService>();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
