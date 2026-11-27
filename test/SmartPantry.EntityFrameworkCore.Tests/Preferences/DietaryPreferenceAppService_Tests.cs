using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using SmartPantry.EntityFrameworkCore;
using SmartPantry.ExternalProducts;
using SmartPantry.Pantries;
using SmartPantry.Products;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Claims;
using Xunit;

namespace SmartPantry.Preferences;

// Parte 2: preferencias (RF-03) y advertencias (RF-12). Las pruebas usan AlwaysAllowAuthorizationService:
// comprueban que cada usuario sólo ve lo suyo, no el 401 sin token (se verifica desde Swagger).
public class DietaryPreferenceAppService_Tests : SmartPantryEntityFrameworkCoreTestBase
{
    // Usuario de FakeCurrentPrincipalAccessor
    private static readonly Guid CurrentUserId = Guid.Parse("2e701e62-0953-4dd3-910b-dc6cc93ccb0d");
    private const string DetailBarcode = "7790580000017";

    private readonly IDietaryPreferenceAppService _preferenceAppService;
    private readonly IPantryItemAppService _pantryItemAppService;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<PreferenceWarning, Guid> _warningRepository;
    private readonly ICurrentPrincipalAccessor _principalAccessor;

    public DietaryPreferenceAppService_Tests()
    {
        _preferenceAppService = GetRequiredService<IDietaryPreferenceAppService>();
        _pantryItemAppService = GetRequiredService<IPantryItemAppService>();
        _productRepository = GetRequiredService<IRepository<Product, Guid>>();
        _warningRepository = GetRequiredService<IRepository<PreferenceWarning, Guid>>();
        _principalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    // ---------- Preferencias ----------

    [Fact]
    public async Task Should_Return_Empty_Preferences_The_First_Time()
    {
        var result = await _preferenceAppService.GetAsync();

        result.WatchedAllergens.ShouldBeEmpty();
        result.WatchedIngredients.ShouldBeEmpty();
        result.ShowAlerts.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Save_And_Read_Preferences()
    {
        await SavePreferencesAsync(new[] { "en:milk" }, new[] { " azúcar " }, showAlerts: false);

        var read = await _preferenceAppService.GetAsync();

        read.WatchedAllergens.ShouldBe(new[] { "en:milk" });
        read.WatchedIngredients.ShouldBe(new[] { "azúcar" });
        read.ShowAlerts.ShouldBeFalse();

        // Guardar otra vez reemplaza la misma fila: hay una sola por usuario.
        await SavePreferencesAsync(new[] { "en:gluten" }, Array.Empty<string>());
        (await _preferenceAppService.GetAsync()).WatchedAllergens.ShouldBe(new[] { "en:gluten" });
        (await WithUnitOfWorkAsync(() => GetRequiredService<IRepository<DietaryPreference, Guid>>().CountAsync()))
            .ShouldBe(1);
    }

    [Fact]
    public async Task Should_Reject_Unknown_Tags_And_Invalid_Words_Without_Saving()
    {
        var unknown = await Should.ThrowAsync<BusinessException>(() =>
            SavePreferencesAsync(new[] { "en:chocolate" }, Array.Empty<string>()));
        unknown.Code.ShouldBe(SmartPantryDomainErrorCodes.UnknownAllergen);

        var repeated = await Should.ThrowAsync<BusinessException>(() =>
            SavePreferencesAsync(Array.Empty<string>(), new[] { "sal", "SAL" }));
        repeated.Code.ShouldBe(SmartPantryDomainErrorCodes.DuplicateIngredient);

        var tooShort = await Should.ThrowAsync<BusinessException>(() =>
            SavePreferencesAsync(Array.Empty<string>(), new[] { "x" }));
        tooShort.Code.ShouldBe(SmartPantryDomainErrorCodes.InvalidIngredient);

        (await _preferenceAppService.GetAsync()).WatchedAllergens.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Keep_Each_Users_Preferences_Apart()
    {
        await SavePreferencesAsync(new[] { "en:milk" }, new[] { "azúcar" });

        using (ChangeUser(Guid.NewGuid()))
        {
            var other = await _preferenceAppService.GetAsync();
            other.WatchedAllergens.ShouldBeEmpty();
            other.WatchedIngredients.ShouldBeEmpty();

            await SavePreferencesAsync(new[] { "en:gluten" }, Array.Empty<string>());
        }

        // Lo que guardó el otro usuario no cambió las propias.
        (await _preferenceAppService.GetAsync()).WatchedAllergens.ShouldBe(new[] { "en:milk" });
    }

    // ---------- Advertencias guardadas (RF-12 "al agregar") ----------

    [Fact]
    public async Task Should_Create_A_Single_Warning_When_Adding_A_Matching_Product()
    {
        await SavePreferencesAsync(new[] { "en:milk" }, Array.Empty<string>());
        var milk = await CreateProductAsync("Leche entera", new[] { "en:milk" }, "Leche entera");

        var added = await AddToPantryAsync(milk);

        added.PreferenceWarning.ShouldNotBeNull();
        added.PreferenceWarning.Status.ShouldBe(PreferenceEvaluationStatus.Match);
        added.PreferenceWarning.MatchedAllergens.ShouldBe(new[] { "en:milk" });
        (await WarningsOfAsync(added.Id)).Count.ShouldBe(1);

        // El listado trae el estado de la advertencia de cada ítem.
        var list = await _pantryItemAppService.GetListAsync(new GetPantryItemListDto());
        list.Items.Single(i => i.Id == added.Id).PreferenceWarning!.Status.ShouldBe(PreferenceEvaluationStatus.Match);
    }

    [Fact]
    public async Task Should_Not_Create_A_Warning_Without_Coincidences()
    {
        await SavePreferencesAsync(new[] { "en:gluten" }, Array.Empty<string>());
        var milk = await CreateProductAsync("Leche entera", new[] { "en:milk" }, "Leche entera");

        var added = await AddToPantryAsync(milk);

        added.PreferenceWarning.ShouldBeNull();
        (await WarningsOfAsync(added.Id)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Mark_As_Not_Verifiable_When_The_Product_Lacks_Data()
    {
        await SavePreferencesAsync(Array.Empty<string>(), new[] { "azúcar" });
        var bread = await CreateProductAsync("Pan casero", null, null);

        var added = await AddToPantryAsync(bread);

        added.PreferenceWarning!.Status.ShouldBe(PreferenceEvaluationStatus.NotVerifiable);
        added.PreferenceWarning.MissingIngredients.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Update_Or_Deactivate_The_Warning_When_Preferences_Change_Without_Duplicating_It()
    {
        await SavePreferencesAsync(new[] { "en:milk" }, Array.Empty<string>());
        var product = await CreateProductAsync("Dulce de leche", new[] { "en:milk" }, "Leche, azúcar");
        var added = await AddToPantryAsync(product);

        // Ahora también vigila una palabra: la misma advertencia suma la coincidencia.
        await SavePreferencesAsync(new[] { "en:milk" }, new[] { "AZUCAR" });
        var updated = (await WarningsOfAsync(added.Id)).ShouldHaveSingleItem();
        updated.IsActive.ShouldBeTrue();
        updated.GetMatchedIngredients().ShouldBe(new[] { "AZUCAR" });

        // Deja de vigilar lo que coincidía: la advertencia se desactiva.
        await SavePreferencesAsync(new[] { "en:gluten" }, Array.Empty<string>());
        var deactivated = (await WarningsOfAsync(added.Id)).ShouldHaveSingleItem();
        deactivated.IsActive.ShouldBeFalse();
        (await _pantryItemAppService.GetAsync(added.Id)).PreferenceWarning.ShouldBeNull();

        // Vuelve a vigilarlo: se reactiva la misma fila.
        await SavePreferencesAsync(new[] { "en:milk" }, Array.Empty<string>());
        var reactivated = (await WarningsOfAsync(added.Id)).ShouldHaveSingleItem();
        reactivated.Id.ShouldBe(updated.Id);
        reactivated.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Hide_Warnings_When_Alerts_Are_Off()
    {
        await SavePreferencesAsync(new[] { "en:milk" }, Array.Empty<string>(), showAlerts: false);
        var milk = await CreateProductAsync("Leche entera", new[] { "en:milk" }, "Leche");

        var added = await AddToPantryAsync(milk);

        // La advertencia se guarda igual (para cuando vuelva a encender las alertas), pero no se muestra.
        added.PreferenceWarning.ShouldBeNull();
        (await WarningsOfAsync(added.Id)).ShouldHaveSingleItem().IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Touch_The_Warnings_Of_Other_Users()
    {
        var milk = await CreateProductAsync("Leche entera", new[] { "en:milk" }, "Leche");
        PantryItemDto othersItem;
        using (ChangeUser(Guid.NewGuid()))
        {
            await SavePreferencesAsync(new[] { "en:milk" }, Array.Empty<string>());
            othersItem = await AddToPantryAsync(milk);
        }

        // El usuario actual deja de vigilar la leche: sólo se recalculan sus propios ítems.
        await SavePreferencesAsync(new[] { "en:gluten" }, Array.Empty<string>());

        (await WarningsOfAsync(othersItem.Id)).ShouldHaveSingleItem().IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Deactivate_The_Warning_When_The_Item_Is_Consumed()
    {
        await SavePreferencesAsync(new[] { "en:milk" }, Array.Empty<string>());
        var milk = await CreateProductAsync("Leche entera", new[] { "en:milk" }, "Leche");
        var added = await AddToPantryAsync(milk);

        await _pantryItemAppService.ConsumeAsync(added.Id);

        (await WarningsOfAsync(added.Id)).ShouldHaveSingleItem().IsActive.ShouldBeFalse();
    }

    // ---------- Evaluación al abrir el detalle (RF-12 "al abrir") ----------

    [Fact]
    public async Task Should_Evaluate_The_Product_Detail_With_The_Current_Users_Preferences()
    {
        GetRequiredService<IExternalProductCatalogClient>()
            .GetByBarcodeAsync(DetailBarcode)
            .Returns(Task.FromResult<ExternalProductDto?>(new ExternalProductDto
            {
                Barcode = DetailBarcode,
                Name = "Galletitas",
                Ingredients = "Harina de trigo, AZÚCAR",
                AllergenTags = new() { "en:gluten" }
            }));
        var externalProducts = GetRequiredService<IExternalProductAppService>();
        var input = new ExternalProductLookupInputDto { Barcode = DetailBarcode };

        // Sin preferencias: se informa para ofrecer configurarlas.
        (await externalProducts.GetByBarcodeAsync(input)).Preferences!.Status
            .ShouldBe(PreferenceEvaluationStatus.NoPreferences);

        await SavePreferencesAsync(new[] { "en:milk" }, new[] { "azucar" });
        var match = (await externalProducts.GetByBarcodeAsync(input)).Preferences!;
        match.Status.ShouldBe(PreferenceEvaluationStatus.Match);
        match.MatchedIngredients.ShouldBe(new[] { "azucar" });

        await SavePreferencesAsync(new[] { "en:milk" }, Array.Empty<string>());
        (await externalProducts.GetByBarcodeAsync(input)).Preferences!.Status
            .ShouldBe(PreferenceEvaluationStatus.NoMatch);

        // Con las alertas apagadas no se evalúa.
        await SavePreferencesAsync(new[] { "en:milk" }, Array.Empty<string>(), showAlerts: false);
        (await externalProducts.GetByBarcodeAsync(input)).Preferences.ShouldBeNull();

        // La evaluación al abrir no se guarda: es sobre un producto que puede no estar en la despensa.
        (await WithUnitOfWorkAsync(() => _warningRepository.CountAsync())).ShouldBe(0);
    }

    // ---------- Ayudas ----------

    private Task<DietaryPreferenceDto> SavePreferencesAsync(
        IEnumerable<string> allergens,
        IEnumerable<string> ingredients,
        bool showAlerts = true)
    {
        return _preferenceAppService.UpdateAsync(new UpdateDietaryPreferenceDto
        {
            WatchedAllergens = allergens.ToList(),
            WatchedIngredients = ingredients.ToList(),
            ShowAlerts = showAlerts
        });
    }

    private async Task<Product> CreateProductAsync(string name, string[]? allergens, string? ingredients)
    {
        return await WithUnitOfWorkAsync(() =>
        {
            var product = new Product(Guid.NewGuid(), name, null);
            product.SetCatalogDetails(null, null, ingredients, allergens, null, null);
            return _productRepository.InsertAsync(product, autoSave: true);
        });
    }

    private Task<PantryItemDto> AddToPantryAsync(Product product)
    {
        return _pantryItemAppService.CreateAsync(new CreatePantryItemDto { ProductId = product.Id });
    }

    private Task<List<PreferenceWarning>> WarningsOfAsync(Guid pantryItemId)
    {
        return WithUnitOfWorkAsync(() => _warningRepository.GetListAsync(w => w.PantryItemId == pantryItemId));
    }

    private IDisposable ChangeUser(Guid userId)
    {
        return _principalAccessor.Change(new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(AbpClaimTypes.UserId, userId.ToString()),
            new Claim(AbpClaimTypes.UserName, "otro-usuario")
        })));
    }
}
