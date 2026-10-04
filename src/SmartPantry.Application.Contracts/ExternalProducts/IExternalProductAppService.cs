using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SmartPantry.ExternalProducts
{
    public interface IExternalProductAppService : IApplicationService
    {
        Task<ExternalProductLookupResultDto> GetProductByBarcodeAsync(GetExternalProductInputDto input);
    }
}