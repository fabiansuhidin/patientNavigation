using Microsoft.Extensions.Options;
using PatientNavigation.Common.Options;
using PatientNavigation.Common.Repositories;
using Procedure.Consumer;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;
        services.Configure<MongoDatabaseSettings>(
            configuration.GetSection(nameof(MongoDatabaseSettings)));
        services.AddSingleton<IMongoDatabaseSettings>(sp =>
            sp.GetRequiredService<IOptions<MongoDatabaseSettings>>().Value);
        services.AddTransient<IProcedureRepository, ProcedureRepository>();
        services.AddTransient<IProcedureService, ProcedureService>();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
