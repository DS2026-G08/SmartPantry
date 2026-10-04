using System.Threading.Tasks;
using SmartPantry.ExternalProducts; // Asegúrate de usar el namespace donde creaste tus DTOs

namespace SmartPantry.ExternalProducts
{
    public interface IExternalProductCatalogClient
    {
        Task<ExternalProductDto?> GetByBarcodeAsync(string barcode);
    }
}