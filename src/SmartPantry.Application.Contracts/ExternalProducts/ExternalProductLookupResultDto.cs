namespace SmartPantry.ExternalProducts
{
    public class ExternalProductLookupResultDto
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public ExternalProductDto? Product { get; set; }
    }
}