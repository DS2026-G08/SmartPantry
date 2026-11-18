using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SmartPantry.ExternalProducts;

public interface IExternalProductAppService : IApplicationService
{
    /// <summary>RF-05 y RF-07: producto y detalle por código de barras.</summary>
    Task<ExternalProductLookupResultDto> GetByBarcodeAsync(ExternalProductLookupInputDto input);

    /// <summary>
    /// RF-06: búsqueda paginada por nombre, marca o categoría. Se llama GetListAsync por la convención
    /// de ABP: así se publica como GET /api/app/external-product (una búsqueda no modifica datos).
    /// </summary>
    Task<ExternalProductSearchResultDto> GetListAsync(ExternalProductSearchInputDto input);
}
