using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using PatientNavigation.Common.KakfaHelpers;
using PatientNavigation.Common.Options;
using PatientNavigation.Common.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddNewtonsoftJson();
JsonConvert.DefaultSettings = (() =>
{
    var settings = new JsonSerializerSettings();
    settings.Converters.Add(new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() });
    return settings;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<MedicationsMongoDatabaseSettings>(
        builder.Configuration.GetSection(nameof(MedicationsMongoDatabaseSettings)));
builder.Services.Configure<MedicationStatementsMongoDatabaseSettings>(
        builder.Configuration.GetSection(nameof(MedicationStatementsMongoDatabaseSettings)));

builder.Services.AddSingleton<IMedicationsMongoDatabaseSettings>(sp =>
    sp.GetRequiredService<IOptions<MedicationsMongoDatabaseSettings>>().Value);
builder.Services.AddSingleton<IMedicationStatementsMongoDatabaseSettings>(sp =>
    sp.GetRequiredService<IOptions<MedicationStatementsMongoDatabaseSettings>>().Value);

builder.Services.AddTransient<IMedicationRepository, MedicationRepository>();
builder.Services.AddTransient<IMedicationStatementRepository, MedicationStatementRepository>();

var bootstrapServer = builder.Configuration.GetValue<string>("KafkaConfig:Servers");
var medicationTopicName = builder.Configuration.GetValue<string>("KafkaConfig:MedicationTopicName");
var medicationStatementTopicName = builder.Configuration.GetValue<string>("KafkaConfig:MedicationStatementTopicName");

await AdminKafkaHelper.CreateTopicAsync(bootstrapServer, medicationTopicName).ConfigureAwait(false);
await AdminKafkaHelper.CreateTopicAsync(bootstrapServer, medicationStatementTopicName).ConfigureAwait(false);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
