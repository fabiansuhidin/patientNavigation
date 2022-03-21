using Medications.Consumer;
using Microsoft.Extensions.Options;
using PatientNavigation.Common.Options;
using PatientNavigation.Common.Repositories;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;
        
        services.Configure<MedicationsMongoDatabaseSettings>(
            configuration.GetSection(nameof(MedicationsMongoDatabaseSettings)));
        services.Configure<MedicationStatementsMongoDatabaseSettings>(
            configuration.GetSection(nameof(MedicationStatementsMongoDatabaseSettings)));
        
        services.AddSingleton<IMedicationsMongoDatabaseSettings>(sp =>
            sp.GetRequiredService<IOptions<MedicationsMongoDatabaseSettings>>().Value);
        services.AddSingleton<IMedicationStatementsMongoDatabaseSettings>(sp =>
            sp.GetRequiredService<IOptions<MedicationStatementsMongoDatabaseSettings>>().Value);
        
        services.AddTransient<IMedicationRepository, MedicationRepository>();
        services.AddTransient<IMedicationStatementRepository, MedicationStatementRepository>();
        
        services.AddTransient<IMedicationService, MedicationService>();
        services.AddTransient<IMedicationStatementService, MedicationStatementService>();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
