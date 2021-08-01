using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace AzureFunctions
{
    public static class OrderItemsReserverQueueConsumer
    {
        [FunctionName("OrderItemsReserverQueueConsumer")]
        public static async Task<IActionResult> Run(
            [ServiceBusTrigger("order-items", Connection = "OrderItemReserveServiceBusUrl")] string orderItemJson, 
            ILogger log, 
            ExecutionContext context)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {orderItemJson}");

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

            return new OkObjectResult("OrderItemsReserverQueueConsumer function executed successfully!");
        }

        private static CloudStorageAccount GetCloudStorageAccount(ExecutionContext executionContext)
        {
            var config = new ConfigurationBuilder()
                            .SetBasePath(executionContext.FunctionAppDirectory)
                            .AddJsonFile("local.settings.json", true, true)
                            .AddEnvironmentVariables().Build();
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(config["CloudStorageAccount"]);
            return storageAccount;
        }
    }
}
