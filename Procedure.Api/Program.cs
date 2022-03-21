using Microsoft.Extensions.Options;
using PatientNavigation.Common.KakfaHelpers;
using PatientNavigation.Common.Options;
using PatientNavigation.Common.Repositories;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(opts =>
    {
        var enumConverter = new JsonStringEnumConverter();
        opts.JsonSerializerOptions.Converters.Add(enumConverter);
    });

var bootstrapServer = builder.Configuration.GetValue<string>("KafkaConfig:Servers");
var topicName = builder.Configuration.GetValue<string>("KafkaConfig:TopicName");
await AdminKafkaHelper.CreateTopicAsync(bootstrapServer, topicName).ConfigureAwait(false);

builder.Services.Configure<MongoDatabaseSettings>(
        builder.Configuration.GetSection(nameof(MongoDatabaseSettings)));
builder.Services.AddSingleton<IMongoDatabaseSettings>(sp =>
    sp.GetRequiredService<IOptions<MongoDatabaseSettings>>().Value);
builder.Services.AddTransient<IProcedureRepository, ProcedureRepository>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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