using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.eShopWeb.ApplicationCore.Services
{
    public sealed class OrderItemReserveService : IOrderItemReserveService
    {
        private readonly HttpClient _httpClient;
        private readonly Uri _functionEndpoint;
        private readonly IAsyncRepository<Basket> _basketRepository;

        public OrderItemReserveService(HttpClient httpClient, Uri functionEndpoint, IAsyncRepository<Basket> basketRepository)
        {
            _httpClient = httpClient;
            _functionEndpoint = functionEndpoint;
            _basketRepository = basketRepository;
        }
        
        public async Task ReserveOrderItemAsync(int basketId)
        {
            var basketSpec = new BasketWithItemsSpecification(basketId);
            var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec).ConfigureAwait(false);

            Guard.Against.NullBasket(basketId, basket);
            Guard.Against.EmptyBasketOnCheckout(basket.Items);

            var itemsToReserve = new List<dynamic>();
            foreach (var item in basket.Items)
            {
                itemsToReserve.Add(new { ItemId = item.CatalogItemId, Quantity = item.Quantity });
            }

            var content = new StringContent(JsonSerializer.Serialize(itemsToReserve));

            await _httpClient.PostAsync(_functionEndpoint, content).ConfigureAwait(false);
        }
    }
}
