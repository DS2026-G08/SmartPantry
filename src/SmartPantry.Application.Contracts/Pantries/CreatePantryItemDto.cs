using System;

namespace SmartPantry.Pantries;

// RF-10 (sólo BackEnd en el TP10): la pantalla de alta queda fuera de alcance.
public class CreatePantryItemDto : UpdatePantryItemDto
{
    public Guid ProductId { get; set; }
}
