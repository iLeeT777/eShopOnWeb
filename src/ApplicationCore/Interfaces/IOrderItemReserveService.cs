using System.Threading.Tasks;

namespace Microsoft.eShopWeb.ApplicationCore.Interfaces
{
    public interface IOrderItemReserveService
    {
        Task ReserveOrderItemAsync(int basketId);
    }
}
