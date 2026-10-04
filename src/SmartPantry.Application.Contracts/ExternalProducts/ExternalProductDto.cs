namespace SmartPantry.ExternalProducts
{
    public class ExternalProductDto
    {
        // El código de barras no es anulable porque es el criterio de búsqueda principal
        public string Barcode { get; set; } = null!;

        // Propiedades anulables para contemplar respuestas incompletas de la API (RF-09)
        public string? Name { get; set; }
        public string? Brand { get; set; }
        public string? ImageUrl { get; set; }
        public string? Quantity { get; set; }
        public string? Categories { get; set; }
    }
}