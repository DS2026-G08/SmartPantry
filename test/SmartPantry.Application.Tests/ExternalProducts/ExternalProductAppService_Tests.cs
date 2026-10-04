using NSubstitute;
using Shouldly;
using System.Threading.Tasks;
using Xunit;
using System.Net.Http; // Necesario para simular las excepciones HTTP
using NSubstitute.ExceptionExtensions; // Necesario para .Throws()

namespace SmartPantry.ExternalProducts
{
    public class ExternalProductAppService_Tests
    {
        private readonly IExternalProductAppService _externalProductAppService;
        private readonly IExternalProductCatalogClient _mockClient;

        public ExternalProductAppService_Tests()
        {
            _mockClient = Substitute.For<IExternalProductCatalogClient>();
            _externalProductAppService = new ExternalProductAppService(_mockClient);
        }

        [Fact]
        public async Task Should_Return_Product_When_Found()
        {
            var expectedDto = new ExternalProductDto
            {
                Barcode = "3017620422003",
                Name = "Nutella",
                Brand = "Ferrero"
            };
            _mockClient.GetByBarcodeAsync("3017620422003").Returns(expectedDto);

            var input = new GetExternalProductInputDto { Barcode = "3017620422003" };

            var result = await _externalProductAppService.GetProductByBarcodeAsync(input);

            // Ahora verificamos IsSuccess y accedemos a los datos mediante result.Product
            result.IsSuccess.ShouldBeTrue();
            result.Product.ShouldNotBeNull();
            result.Product.Name.ShouldBe("Nutella");
            result.Product.Brand.ShouldBe("Ferrero");
        }

        [Fact]
        public async Task Should_Return_Failure_When_Product_Not_Found()
        {
            _mockClient.GetByBarcodeAsync("0000000000000").Returns((ExternalProductDto?)null);

            var input = new GetExternalProductInputDto { Barcode = "0000000000000" };

            var result = await _externalProductAppService.GetProductByBarcodeAsync(input);

            // Verificamos que indique el error controladamente en lugar de lanzar excepción
            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("0000000000000");
            result.Product.ShouldBeNull();
        }

        [Fact]
        public async Task Should_Handle_Incomplete_Data_Correctly()
        {
            var expectedDto = new ExternalProductDto
            {
                Barcode = "12345678",
                Name = "Producto Raro",
                Brand = null,
                ImageUrl = null
            };
            _mockClient.GetByBarcodeAsync("12345678").Returns(expectedDto);

            var input = new GetExternalProductInputDto { Barcode = "12345678" };

            var result = await _externalProductAppService.GetProductByBarcodeAsync(input);

            result.IsSuccess.ShouldBeTrue();
            result.Product.ShouldNotBeNull();
            result.Product.Name.ShouldBe("Producto Raro");
            result.Product.Brand.ShouldBeNull();
            result.Product.ImageUrl.ShouldBeNull();
        }

        [Fact]
        public async Task Should_Handle_Rate_Limit_Correctly()
        {
            // Simulamos el error 429 Too Many Requests
            _mockClient.GetByBarcodeAsync(Arg.Any<string>())
                .Throws(new HttpRequestException("Too Many Requests", null, System.Net.HttpStatusCode.TooManyRequests));

            var input = new GetExternalProductInputDto { Barcode = "12345678" };

            // Verificamos que la excepción HTTP se propague correctamente
            await Assert.ThrowsAsync<HttpRequestException>(async () =>
                await _externalProductAppService.GetProductByBarcodeAsync(input));
        }

        [Fact]
        public async Task Should_Handle_Provider_Unavailable()
        {
            // Simulamos el error 503 Service Unavailable
            _mockClient.GetByBarcodeAsync(Arg.Any<string>())
                .Throws(new HttpRequestException("Service Unavailable", null, System.Net.HttpStatusCode.ServiceUnavailable));

            var input = new GetExternalProductInputDto { Barcode = "12345678" };

            await Assert.ThrowsAsync<HttpRequestException>(async () =>
                await _externalProductAppService.GetProductByBarcodeAsync(input));
        }
    }
}