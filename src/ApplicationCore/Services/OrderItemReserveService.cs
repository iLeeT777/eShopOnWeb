using Ardalis.GuardClauses;
using Microsoft.Azure.ServiceBus;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.eShopWeb.ApplicationCore.Services
{
    public sealed class OrderItemReserveService : IOrderItemReserveService
    {
        private readonly IQueueClient _queueClient;
        private readonly IAsyncRepository<Basket> _basketRepository;

        public OrderItemReserveService(
            IQueueClient queueClient, 
            IAsyncRepository<Basket> basketRepository)
        {
            _queueClient = queueClient;
            _basketRepository = basketRepository;
        }
        
        public async Task ReserveOrderItemAsync(int basketId)
        {
            var basketSpec = new BasketWithItemsSpecification(basketId);
            var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec).ConfigureAwait(false);

            Guard.Against.NullBasket(basketId, basket);
            Guard.Against.EmptyBasketOnCheckout(basket.Items);

            var itemsToReserve = new List<dynamic>();
            itemsToReserve.Add(new { CreatedTime = $"{DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss")}" });
            foreach (var item in basket.Items)
            {
                itemsToReserve.Add(new { ItemId = item.CatalogItemId, Quantity = item.Quantity });
            }

            var itemsJson = JsonSerializer.Serialize(itemsToReserve);

            await _queueClient.SendAsync(new Message(Encoding.UTF8.GetBytes(itemsJson))).ConfigureAwait(false);
        }
    }
}
