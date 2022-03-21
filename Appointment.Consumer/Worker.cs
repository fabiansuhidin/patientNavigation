using System.Text;
using Confluent.Kafka;

namespace Appointment.Consumer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAppointmentService _appointmentService;
        private readonly string _topicName;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IAppointmentService appointmentService)
        {
            _logger = logger;
            _configuration = configuration;
            _appointmentService = appointmentService;
            _topicName = _configuration.GetValue<string>("KafkaConfig:TopicName");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _configuration.GetValue<string>("KafkaConfig:Servers"),
                GroupId = "appointment",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            while (!stoppingToken.IsCancellationRequested)
            {
                using var consumer = new ConsumerBuilder<Null, string>(config).Build();
                
                consumer.Subscribe(_topicName);
                Console.WriteLine($"Subscribed to {_topicName}");

                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    _logger.LogInformation($"Consumed event: {consumeResult?.Message.Value}");
                    
                    if (consumeResult?.Message?.Value is null)
                        continue;
                    
                    var headerActionBytes = consumeResult.Message.Headers.FirstOrDefault(a => a.Key.ToLower() == "action")?.GetValueBytes();
                    if (headerActionBytes != null)
                    {
                        var value = Encoding.UTF8.GetString(headerActionBytes);
                        if (value.ToLower() == "sync")
                            await _appointmentService.SyncAppointment(consumeResult.Message.Value);
                        continue;
                    }
                    await _appointmentService.InsertAppointment(consumeResult.Message.Value);
                }

                consumer.Close();
                await Task.Delay(1000);
            }
        }
    }
}