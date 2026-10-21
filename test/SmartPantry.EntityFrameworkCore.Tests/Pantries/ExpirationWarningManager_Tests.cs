using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using SmartPantry.EntityFrameworkCore;
using SmartPantry.Products;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace SmartPantry.Pantries;

public class ExpirationWarningManager_Tests : SmartPantryEntityFrameworkCoreTestBase
{
    // Fecha controlada: las pruebas no dependen del reloj ni del temporizador del worker.
    private static readonly DateOnly Today = new(2026, 10, 21);

    private readonly ExpirationWarningManager _manager;
    private readonly IRepository<Pantry, Guid> _pantryRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<PantryItemWarning, Guid> _warningRepository;

    public ExpirationWarningManager_Tests()
    {
        _manager = GetRequiredService<ExpirationWarningManager>();
        _pantryRepository = GetRequiredService<IRepository<Pantry, Guid>>();
        _productRepository = GetRequiredService<IRepository<Product, Guid>>();
        _warningRepository = GetRequiredService<IRepository<PantryItemWarning, Guid>>();
    }

    [Fact]
    public async Task Should_Warn_Only_Items_Inside_Threshold_On_Limit_Day_Or_Expired()
    {
        var inside = await CreateItemAsync(Today.AddDays(1));
        var limitDay = await CreateItemAsync(Today.AddDays(ExpirationWarningConsts.ThresholdDays));
        var outside = await CreateItemAsync(Today.AddDays(ExpirationWarningConsts.ThresholdDays + 1));
        var expired = await CreateItemAsync(Today.AddDays(-2));
        var withoutDate = await CreateItemAsync(null);
        var consumed = await CreateItemAsync(Today.AddDays(1), consumed: true);

        var result = await ProcessAsync();

        result.ShouldBe(new ExpirationWarningProcessResult(Created: 3, Updated: 0, Reactivated: 0, Deactivated: 0));
        (await GetWarningAsync(inside)).ShouldNotBeNull().IsActive.ShouldBeTrue();
        (await GetWarningAsync(limitDay)).ShouldNotBeNull().IsActive.ShouldBeTrue();
        (await GetWarningAsync(expired)).ShouldNotBeNull().ExpirationDate.ShouldBe(Today.AddDays(-2));
        (await GetWarningAsync(outside)).ShouldBeNull();
        (await GetWarningAsync(withoutDate)).ShouldBeNull();
        (await GetWarningAsync(consumed)).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Keep_A_Single_Warning_Per_Item_When_Processed_Twice()
    {
        var first = await CreateItemAsync(Today.AddDays(1));
        var second = await CreateItemAsync(Today.AddDays(-1));

        await ProcessAsync();
        var secondRun = await ProcessAsync();

        secondRun.HasChanges.ShouldBeFalse();
        var warnings = await WithUnitOfWorkAsync(() => _warningRepository.GetListAsync());
        warnings.Count.ShouldBe(2);
        warnings.Count(w => w.PantryItemId == first).ShouldBe(1);
        warnings.Count(w => w.PantryItemId == second).ShouldBe(1);
    }

    [Fact]
    public async Task Should_Update_Deactivate_And_Reactivate_The_Same_Warning()
    {
        var itemId = await CreateItemAsync(Today.AddDays(2));
        await ProcessAsync();
        var original = (await GetWarningAsync(itemId)).ShouldNotBeNull();

        // Cambia la fecha dentro del umbral: se actualiza la misma advertencia.
        await ChangeExpirationDateAsync(itemId, Today.AddDays(1));
        (await ProcessAsync()).Updated.ShouldBe(1);
        var updated = (await GetWarningAsync(itemId)).ShouldNotBeNull();
        updated.Id.ShouldBe(original.Id);
        updated.ExpirationDate.ShouldBe(Today.AddDays(1));
        updated.IsActive.ShouldBeTrue();

        // Sale del umbral: se desactiva, no se borra.
        await ChangeExpirationDateAsync(itemId, Today.AddDays(10));
        (await ProcessAsync()).Deactivated.ShouldBe(1);
        var deactivated = (await GetWarningAsync(itemId)).ShouldNotBeNull();
        deactivated.Id.ShouldBe(original.Id);
        deactivated.IsActive.ShouldBeFalse();

        // Vuelve a entrar: se reactiva la misma fila.
        await ChangeExpirationDateAsync(itemId, Today.AddDays(ExpirationWarningConsts.ThresholdDays));
        (await ProcessAsync()).Reactivated.ShouldBe(1);
        var reactivated = (await GetWarningAsync(itemId)).ShouldNotBeNull();
        reactivated.Id.ShouldBe(original.Id);
        reactivated.IsActive.ShouldBeTrue();
        reactivated.ExpirationDate.ShouldBe(Today.AddDays(ExpirationWarningConsts.ThresholdDays));
    }

    [Fact]
    public async Task Should_Deactivate_Warning_When_Item_Is_Consumed_Or_Loses_Its_Date()
    {
        var consumedItem = await CreateItemAsync(Today.AddDays(1));
        var itemWithoutDate = await CreateItemAsync(Today.AddDays(1));
        await ProcessAsync();

        await WithUnitOfWorkAsync(async () =>
        {
            var pantry = await GetPantryOfItemAsync(consumedItem);
            pantry.ConsumeItem(consumedItem);
            await _pantryRepository.UpdateAsync(pantry);
        });
        await ChangeExpirationDateAsync(itemWithoutDate, null);

        (await ProcessAsync()).Deactivated.ShouldBe(2);
        (await GetWarningAsync(consumedItem)).ShouldNotBeNull().IsActive.ShouldBeFalse();
        (await GetWarningAsync(itemWithoutDate)).ShouldNotBeNull().IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Reject_A_Second_Warning_For_The_Same_Item_And_Type()
    {
        // Simula una ejecución cercana que no vio la advertencia ya guardada por otra:
        // el índice único de la base impide el duplicado.
        var itemId = await CreateItemAsync(Today.AddDays(1));
        await ProcessAsync();

        await Should.ThrowAsync<DbUpdateException>(async () =>
        {
            await WithUnitOfWorkAsync(() => _warningRepository.InsertAsync(
                new PantryItemWarning(Guid.NewGuid(), itemId, PantryItemWarningType.Expiration, Today.AddDays(1)),
                autoSave: true));
        });

        var warnings = await WithUnitOfWorkAsync(() => _warningRepository.GetListAsync(w => w.PantryItemId == itemId));
        warnings.Count.ShouldBe(1);
    }

    private Task<ExpirationWarningProcessResult> ProcessAsync()
    {
        return WithUnitOfWorkAsync(() => _manager.ProcessAsync(Today));
    }

    // Cada ítem queda en una despensa de un propietario distinto: el proceso no depende del usuario actual.
    private Task<Guid> CreateItemAsync(DateOnly? expirationDate, bool consumed = false)
    {
        return WithUnitOfWorkAsync(async () =>
        {
            var product = await _productRepository.InsertAsync(
                new Product(Guid.NewGuid(), "Leche", "La Serenisima", "7790742000001"));

            var pantry = new Pantry(Guid.NewGuid(), Guid.NewGuid(), "Despensa de prueba");
            var item = pantry.AddItem(Guid.NewGuid(), product.Id, expirationDate);
            if (consumed)
            {
                pantry.ConsumeItem(item.Id);
            }

            await _pantryRepository.InsertAsync(pantry);
            return item.Id;
        });
    }

    private Task ChangeExpirationDateAsync(Guid itemId, DateOnly? expirationDate)
    {
        return WithUnitOfWorkAsync(async () =>
        {
            var pantry = await GetPantryOfItemAsync(itemId);
            pantry.ChangeItemExpirationDate(itemId, expirationDate);
            await _pantryRepository.UpdateAsync(pantry);
        });
    }

    private Task<Pantry> GetPantryOfItemAsync(Guid itemId)
    {
        return _pantryRepository.GetAsync(p => p.Items.Any(i => i.Id == itemId));
    }

    private Task<PantryItemWarning?> GetWarningAsync(Guid itemId)
    {
        return WithUnitOfWorkAsync(() => _warningRepository.FindAsync(w =>
            w.PantryItemId == itemId && w.Type == PantryItemWarningType.Expiration));
    }
}
