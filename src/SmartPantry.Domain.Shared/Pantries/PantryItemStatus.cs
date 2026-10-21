namespace SmartPantry.Pantries;

public enum PantryItemStatus
{
    /// <summary>El ítem está en la despensa.</summary>
    Active = 1,

    /// <summary>El ítem ya se consumió: no corresponde avisar su vencimiento.</summary>
    Consumed = 2
}
