using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using SmartPantry.Pantries;
using SmartPantry.Products;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;

namespace SmartPantry.DbMigrator.Pantries;

/// <summary>
/// TP08: despensa de ejemplo para ver el worker de vencimientos en la base local.
/// Vive en DbMigrator para no cargar datos en la suite de pruebas. El propietario es
/// el usuario admin creado por el seeding de Identity; no hay API para elegirlo.
/// </summary>
public class PantryDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private const string SamplePantryName = "Despensa de prueba";

    private readonly IRepository<Pantry, Guid> _pantryRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IIdentityUserRepository _userRepository;
    private readonly ILookupNormalizer _lookupNormalizer;
    private readonly IGuidGenerator _guidGenerator;

    public PantryDataSeedContributor(
        IRepository<Pantry, Guid> pantryRepository,
        IRepository<Product, Guid> productRepository,
        IIdentityUserRepository userRepository,
        ILookupNormalizer lookupNormalizer,
        IGuidGenerator guidGenerator)
    {
        _pantryRepository = pantryRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _lookupNormalizer = lookupNormalizer;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        var admin = await _userRepository.FindByNormalizedUserNameAsync(
            _lookupNormalizer.NormalizeName(IdentityDataSeedContributor.AdminUserNameDefaultValue));

        if (admin is null || await _pantryRepository.AnyAsync(p => p.OwnerId == admin.Id))
        {
            return;
        }

        var leche = await GetOrCreateProductAsync("Leche Entera", "La Serenisima", "7790742000001");
        var yogur = await GetOrCreateProductAsync("Yogur Bebible", "Ser", "7791337000002");
        var queso = await GetOrCreateProductAsync("Queso Cremoso", "Sancor", "7790080000003");
        var pan = await GetOrCreateProductAsync("Pan Lactal", "Fargo", "7793890000004");
        var arroz = await GetOrCreateProductAsync("Arroz Largo Fino", "Gallo", "7790070000005");
        var manteca = await GetOrCreateProductAsync("Manteca", "La Serenisima", "7790742000006");

        // Fechas relativas al día en que se corre DbMigrator, para cubrir todos los casos de la regla.
        var today = ArgentinaCalendar.GetToday(DateTime.UtcNow);
        var pantry = new Pantry(_guidGenerator.Create(), admin.Id, SamplePantryName);

        pantry.AddItem(_guidGenerator.Create(), leche.Id, today.AddDays(1));    // dentro del umbral
        pantry.AddItem(_guidGenerator.Create(), yogur.Id, today.AddDays(ExpirationWarningConsts.ThresholdDays)); // día límite
        pantry.AddItem(_guidGenerator.Create(), queso.Id, today.AddDays(10));   // fuera del umbral
        pantry.AddItem(_guidGenerator.Create(), pan.Id, today.AddDays(-2));     // vencido
        pantry.AddItem(_guidGenerator.Create(), arroz.Id, null);                // sin fecha
        var consumed = pantry.AddItem(_guidGenerator.Create(), manteca.Id, today.AddDays(1));
        pantry.ConsumeItem(consumed.Id);                                        // consumido

        await _pantryRepository.InsertAsync(pantry, autoSave: true);
    }

    private async Task<Product> GetOrCreateProductAsync(string name, string brand, string barcode)
    {
        var product = await _productRepository.FindAsync(p => p.Barcode == barcode);
        if (product is not null)
        {
            return product;
        }

        return await _productRepository.InsertAsync(
            new Product(_guidGenerator.Create(), name, brand, barcode),
            autoSave: true);
    }
}
