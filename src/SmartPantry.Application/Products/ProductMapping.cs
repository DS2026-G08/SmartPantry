using System.Linq;

namespace SmartPantry.Products;

/// <summary>Convierte un producto a su DTO. Manual porque los alérgenos se guardan como texto y se exponen como lista.</summary>
public static class ProductMapping
{
    public static ProductDto ToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Brand = product.Brand,
            Barcode = product.Barcode,
            ImageUrl = product.ImageUrl,
            Categories = product.Categories,
            Ingredients = product.Ingredients,
            Allergens = product.GetAllergenTags().ToList(),
            NutriScore = product.NutriScore,
            NovaGroup = product.NovaGroup
        };
    }
}
