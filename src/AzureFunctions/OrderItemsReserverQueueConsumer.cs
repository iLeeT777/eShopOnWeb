using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;
using System.Text;
using Polly;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using System.Collections.Generic;

namespace AzureFunctions
{
    public static class OrderItemsReserverQueueConsumer
    {
        private const int RetryCount = 3;
        private const int TimeBetweenRetries = 10; // in seconds

        [FunctionName("OrderItemsReserverQueueConsumer")]
        public static async Task Run(
            [ServiceBusTrigger("order-items", Connection = "OrderItemReserveServiceBusUrl")] string orderItemJson, 
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {orderItemJson}");

            var uploadToStorageAction = await Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(RetryCount, _ => TimeSpan.FromSeconds(TimeBetweenRetries))
                .ExecuteAndCaptureAsync(async () =>
                {
                    CloudStorageAccount storageAccount = GetCloudStorageAccount(context);
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer container = blobClient.GetContainerReference("eshop-blob-container");

                    await container.CreateIfNotExistsAsync();

                    var fileName = $"{DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss")} Order #{Guid.NewGuid():n}.json";

                    var blob = container.GetBlockBlobReference(fileName);
                    blob.Properties.ContentType = "application/json";

                    using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(orderItemJson)))
                    {
                        await blob.UploadFromStreamAsync(stream);
                    }
                });

            if(uploadToStorageAction.Outcome == OutcomeType.Failure)
            {
                log.LogError("Unable to store the order in blob storage.");

                var config = CreateConfig(context);
                TopicCredentials topicCredentials = new TopicCredentials(config["EventGridAccessKey"]);
                EventGridClient client = new EventGridClient(topicCredentials);

                var eventGridEvent = new EventGridEvent()
                {
                    Id = Guid.NewGuid().ToString(),
                    EventType = "AzureFunctions.OrderItems.ItemError",
                    Data = orderItemJson,
                    EventTime = DateTime.UtcNow,
                    Subject = $"Unable to store the order in blob storage. Reason: {uploadToStorageAction.FinalException?.Message}",
                    DataVersion = "1.0"
                };

                await client.PublishEventsAsync(config["EventGridTopicEndpoint"], new List<EventGridEvent> { eventGridEvent });
            }
        }

        private static CloudStorageAccount GetCloudStorageAccount(ExecutionContext executionContext)
        {
            var config = CreateConfig(executionContext);
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(config["CloudStorageAccount"]);
            return storageAccount;
        }

        private static IConfigurationRoot CreateConfig(ExecutionContext executionContext)
        {
            return new ConfigurationBuilder()
                            .SetBasePath(executionContext.FunctionAppDirectory)
                            .AddJsonFile("local.settings.json", true, true)
                            .AddEnvironmentVariables().Build();
        }
    }
}
