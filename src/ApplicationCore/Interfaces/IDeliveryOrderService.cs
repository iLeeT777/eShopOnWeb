using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using System.Threading.Tasks;

namespace Microsoft.eShopWeb.ApplicationCore.Interfaces
{
    public interface IDeliveryOrderService
    {
        Task PublishDeliveryOrderAsync(Order order);
    }
}
