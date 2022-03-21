using Microsoft.Extensions.Options;
using Patient.Consumer;
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
        services.AddTransient<IPatientRepository, PatientRepository>();
        services.AddTransient<IPatientService, PatientService>();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
