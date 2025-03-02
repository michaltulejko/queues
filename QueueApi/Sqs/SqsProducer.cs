using Amazon.SQS;
using Amazon.SQS.Model;
using QueueApi.Sqs.Interfaces;

namespace QueueApi.Sqs
{
    public class SqsProducer(IAmazonSQS amazonSqs) : ISqsProducer
    {
        public Task ProduceAsync(string topic, Guid key, long timestamp, CancellationToken cancellationToken = default)
        {
            var request = new SendMessageRequest
            {
                MessageBody = timestamp.ToString()
            };

            return amazonSqs.SendMessageAsync(request, cancellationToken);
        }
    }
}
