using Confluent.Kafka;

namespace PatientNavigation.Common.KakfaHelpers
{
    public class EventsHelper
    {
        private string _boostrapServers;
        private readonly ClientConfig _clientConfig;
        public EventsHelper(string boostrapServers)
        {
            _boostrapServers = boostrapServers;
            _clientConfig = new()
            {
                BootstrapServers = _boostrapServers,
                SecurityProtocol = SecurityProtocol.Plaintext
            };
        }

        public async Task<DeliveryResult<Null, string>?> Produce(string topicName, string message, Headers? headers = null)
        {
            using var producer = new ProducerBuilder<Null, string>(_clientConfig).Build();
            return await producer.ProduceAsync(topicName, new Message<Null, string> { Value = message, Headers = headers}).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Console.WriteLine("An error has occured during publishing process. Details: " + task?.Exception?.Message ?? string.Empty);
                    return task?.Result;
                }
                Console.WriteLine($"Wrote to offset: {task.Result.Offset}");
                return task.Result;
            });
        }
    }
}