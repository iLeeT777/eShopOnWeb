using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.eShopWeb.ApplicationCore.Services
{
    public sealed class DeliveryOrderService : IDeliveryOrderService
    {
        private readonly HttpClient _httpClient;
        private readonly Uri _functionEndpoint;

        public DeliveryOrderService(HttpClient httpClient, Uri functionEndpoint)
        {
            _httpClient = httpClient;
            _functionEndpoint = functionEndpoint;
        }

        public async Task PublishDeliveryOrderAsync(Order order)
        {
            Guard.Against.Null(order, nameof(order));

            dynamic orderToPublish = new { ShippingAddress = order.ShipToAddress, Items = order.OrderItems, Price = order.Total() };

            var content = new StringContent(JsonSerializer.Serialize(orderToPublish));

            await _httpClient.PostAsync(_functionEndpoint, content).ConfigureAwait(false);
        }
    }
}
