using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using SmartPantry.EntityFrameworkCore;
using SmartPantry.Products;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Validation;
using Xunit;

namespace SmartPantry.Pantries;

// Las pruebas usan AlwaysAllowAuthorizationService: comprueban el filtro por propietario del
// servicio, no el acceso HTTP (401 sin token se verifica desde Swagger).
public class PantryItemAppService_Tests : SmartPantryEntityFrameworkCoreTestBase
{
    // Usuario de FakeCurrentPrincipalAccessor
    private static readonly Guid CurrentUserId = Guid.Parse("2e701e62-0953-4dd3-910b-dc6cc93ccb0d");
    private static readonly Guid OtherUserId = Guid.NewGuid();
    private static readonly DateOnly SomeDate = new(2026, 11, 10);

    private readonly IPantryItemAppService _pantryItemAppService;
    private readonly IRepository<Pantry, Guid> _pantryRepository;
    private readonly IRepository<Product, Guid> _productRepository;

    public PantryItemAppService_Tests()
    {
        _pantryItemAppService = GetRequiredService<IPantryItemAppService>();
        _pantryRepository = GetRequiredService<IRepository<Pantry, Guid>>();
        _productRepository = GetRequiredService<IRepository<Product, Guid>>();
    }

    [Fact]
    public async Task Should_List_Only_Own_Active_Items_With_Product_Name()
    {
        var own = await CreateItemAsync(CurrentUserId, "Leche", SomeDate, 2, "l");
        var withoutData = await CreateItemAsync(CurrentUserId, "Arroz", null);
        await CreateItemAsync(CurrentUserId, "Manteca", SomeDate, consumed: true);
        await CreateItemAsync(OtherUserId, "Queso", SomeDate);

        var result = await _pantryItemAppService.GetListAsync(new GetPantryItemListDto());

        result.TotalCount.ShouldBe(2);
        result.Items.Select(i => i.Id).ShouldBe(new[] { own, withoutData }); // con fecha primero
        var leche = result.Items[0];
        leche.ProductName.ShouldBe("Leche");
        leche.Quantity.ShouldBe(2);
        leche.Unit.ShouldBe("l");

        // RF-09: los datos no informados llegan como null, sin valores inventados.
        var arroz = result.Items[1];
        arroz.ExpirationDate.ShouldBeNull();
        arroz.Quantity.ShouldBeNull();
        arroz.Unit.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Include_Consumed_Items_As_History_When_Requested()
    {
        await CreateItemAsync(CurrentUserId, "Leche", SomeDate);
        var consumed = await CreateItemAsync(CurrentUserId, "Manteca", SomeDate, consumed: true);

        var result = await _pantryItemAppService.GetListAsync(new GetPantryItemListDto { IncludeConsumed = true });

        result.TotalCount.ShouldBe(2);
        result.Items.Last().Id.ShouldBe(consumed); // los consumidos van al final
        result.Items.Last().Status.ShouldBe(PantryItemStatus.Consumed);
    }

    [Fact]
    public async Task Should_Return_A_Page_Of_Own_Items()
    {
        for (var day = 1; day <= 3; day++)
        {
            await CreateItemAsync(CurrentUserId, "Producto " + day, SomeDate.AddDays(day));
        }

        var page = await _pantryItemAppService.GetListAsync(new GetPantryItemListDto { SkipCount = 1, MaxResultCount = 1 });

        page.TotalCount.ShouldBe(3);
        page.Items.Single().ProductName.ShouldBe("Producto 2");
    }

    [Fact]
    public async Task Should_Create_Item_In_A_New_Pantry_Of_The_Current_User()
    {
        var product = await CreateProductAsync("Yerba");

        var created = await _pantryItemAppService.CreateAsync(new CreatePantryItemDto
        {
            ProductId = product.Id,
            ExpirationDate = SomeDate,
            Quantity = 1,
            Unit = " kg "
        });
        await _pantryItemAppService.CreateAsync(new CreatePantryItemDto { ProductId = product.Id });

        created.ProductName.ShouldBe("Yerba");
        created.Unit.ShouldBe("kg");
        created.Status.ShouldBe(PantryItemStatus.Active);

        var pantries = await WithUnitOfWorkAsync(() => _pantryRepository.GetListAsync(p => p.OwnerId == CurrentUserId, includeDetails: true));
        pantries.Count.ShouldBe(1); // el segundo ítem reutiliza la despensa
        pantries[0].Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Update_Own_Item_And_Persist_The_Change()
    {
        var itemId = await CreateItemAsync(CurrentUserId, "Leche", SomeDate);

        var updated = await _pantryItemAppService.UpdateAsync(itemId, new UpdatePantryItemDto
        {
            ExpirationDate = SomeDate.AddDays(5),
            Quantity = 1.5m,
            Unit = "l"
        });

        updated.Quantity.ShouldBe(1.5m);
        var fetched = await _pantryItemAppService.GetAsync(itemId);
        fetched.ExpirationDate.ShouldBe(SomeDate.AddDays(5));
        fetched.Quantity.ShouldBe(1.5m);
        fetched.Unit.ShouldBe("l");
    }

    [Fact]
    public async Task Should_Reject_Negative_Quantity_And_Too_Long_Unit()
    {
        var itemId = await CreateItemAsync(CurrentUserId, "Leche", SomeDate, 2, "l");

        await Should.ThrowAsync<AbpValidationException>(() =>
            _pantryItemAppService.UpdateAsync(itemId, new UpdatePantryItemDto { Quantity = -1 }));
        await Should.ThrowAsync<AbpValidationException>(() =>
            _pantryItemAppService.UpdateAsync(itemId, new UpdatePantryItemDto
            {
                Unit = new string('u', PantryItemConsts.MaxUnitLength + 1)
            }));

        var unchanged = await _pantryItemAppService.GetAsync(itemId);
        unchanged.Quantity.ShouldBe(2);
        unchanged.Unit.ShouldBe("l");
    }

    [Fact]
    public async Task Should_Mark_Own_Item_As_Consumed_Without_Deleting_It()
    {
        var itemId = await CreateItemAsync(CurrentUserId, "Leche", SomeDate);

        var consumed = await _pantryItemAppService.ConsumeAsync(itemId);

        consumed.Status.ShouldBe(PantryItemStatus.Consumed);
        (await _pantryItemAppService.GetListAsync(new GetPantryItemListDto())).TotalCount.ShouldBe(0);
        (await _pantryItemAppService.GetAsync(itemId)).Status.ShouldBe(PantryItemStatus.Consumed);
    }

    [Fact]
    public async Task Should_Delete_Own_Item()
    {
        var itemId = await CreateItemAsync(CurrentUserId, "Leche", SomeDate);

        await _pantryItemAppService.DeleteAsync(itemId);

        await Should.ThrowAsync<EntityNotFoundException>(() => _pantryItemAppService.GetAsync(itemId));
    }

    [Fact]
    public async Task Should_Not_Read_Or_Modify_An_Item_Of_Another_User()
    {
        var foreignItem = await CreateItemAsync(OtherUserId, "Queso", SomeDate, 1, "kg");

        // Todas las operaciones responden igual que con un ítem inexistente (404).
        await Should.ThrowAsync<EntityNotFoundException>(() => _pantryItemAppService.GetAsync(foreignItem));
        await Should.ThrowAsync<EntityNotFoundException>(() =>
            _pantryItemAppService.UpdateAsync(foreignItem, new UpdatePantryItemDto { Quantity = 99 }));
        await Should.ThrowAsync<EntityNotFoundException>(() => _pantryItemAppService.ConsumeAsync(foreignItem));
        await Should.ThrowAsync<EntityNotFoundException>(() => _pantryItemAppService.DeleteAsync(foreignItem));

        // El ítem ajeno quedó intacto.
        var pantry = await WithUnitOfWorkAsync(() =>
            _pantryRepository.GetAsync(p => p.Items.Any(i => i.Id == foreignItem)));
        var item = pantry.GetItem(foreignItem);
        item.Quantity.ShouldBe(1);
        item.Status.ShouldBe(PantryItemStatus.Active);
    }

    private async Task<Product> CreateProductAsync(string name)
    {
        return await WithUnitOfWorkAsync(() =>
            _productRepository.InsertAsync(new Product(Guid.NewGuid(), name, "Marca"), autoSave: true));
    }

    // Cada ítem queda en su propia despensa: el listado debe juntar todas las del usuario.
    private async Task<Guid> CreateItemAsync(
        Guid ownerId,
        string productName,
        DateOnly? expirationDate,
        decimal? quantity = null,
        string? unit = null,
        bool consumed = false)
    {
        var product = await CreateProductAsync(productName);

        return await WithUnitOfWorkAsync(async () =>
        {
            var pantry = new Pantry(Guid.NewGuid(), ownerId, "Despensa de prueba");
            var item = pantry.AddItem(Guid.NewGuid(), product.Id, expirationDate, quantity, unit);
            if (consumed)
            {
                pantry.ConsumeItem(item.Id);
            }

            await _pantryRepository.InsertAsync(pantry);
            return item.Id;
        });
    }
}
