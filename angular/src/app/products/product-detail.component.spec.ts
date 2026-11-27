import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { NEVER, of, throwError } from 'rxjs';
import { NgxValidateCoreModule } from '@ngx-validate/core';
import { LocalizationPipe } from '@abp/ng.core';
import { CoreTestingModule, LocalizationPipe as MockLocalizationPipe } from '@abp/ng.core/testing';
import { ToasterService } from '@abp/ng.theme.shared';
import { ThemeSharedTestingModule } from '@abp/ng.theme.shared/testing';
import {
  ExternalProductLookupResultDto,
  ExternalProductLookupStatus,
  ExternalProductService,
} from '../proxy/external-products';
import { PantryItemService, PantryItemStatus } from '../proxy/pantries';
import { PreferenceEvaluationStatus } from '../proxy/preferences';
import { ProductService } from '../proxy/products';
import { ProductDetailComponent } from './product-detail.component';

// Los servicios del proxy se reemplazan por mocks: las pruebas no dependen de la red.
describe('ProductDetailComponent', () => {
  const barcode = '7790742363206';

  let fixture: ComponentFixture<ProductDetailComponent>;
  let component: ProductDetailComponent;
  let externalProductService: jasmine.SpyObj<ExternalProductService>;
  let productService: jasmine.SpyObj<ProductService>;
  let pantryItemService: jasmine.SpyObj<PantryItemService>;
  let toaster: jasmine.SpyObj<ToasterService>;

  const milk: ExternalProductLookupResultDto = {
    status: ExternalProductLookupStatus.Found,
    barcode,
    name: 'Leche entera',
    brand: 'La Serenísima',
    imageUrl: null,
    categories: 'Lácteos',
    ingredients: 'Leche entera, vitaminas A y D',
    allergens: ['en:milk', 'en:other-thing'],
    nutriScore: 'b',
    novaGroup: 1,
  };

  function setup(routeBarcode = barcode) {
    TestBed.configureTestingModule({
      imports: [CoreTestingModule.withConfig(), ThemeSharedTestingModule.withConfig(), ProductDetailComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({ barcode: routeBarcode })),
            queryParamMap: of(convertToParamMap({ q: 'leche' })),
          },
        },
        { provide: ExternalProductService, useValue: externalProductService },
        { provide: ProductService, useValue: productService },
        { provide: PantryItemService, useValue: pantryItemService },
        { provide: ToasterService, useValue: toaster },
      ],
    });
    TestBed.overrideComponent(ProductDetailComponent, {
      // Los mensajes visuales de validación dependen de la configuración del tema; acá se verifican los errores del formulario.
      remove: { imports: [LocalizationPipe, NgxValidateCoreModule] },
      add: { imports: [MockLocalizationPipe] },
    });

    fixture = TestBed.createComponent(ProductDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  function query(testId: string): HTMLElement | null {
    return fixture.nativeElement.querySelector(`[data-test="${testId}"]`);
  }

  beforeEach(() => {
    externalProductService = jasmine.createSpyObj<ExternalProductService>('ExternalProductService', ['getByBarcode']);
    productService = jasmine.createSpyObj<ProductService>('ProductService', ['saveFromExternal']);
    pantryItemService = jasmine.createSpyObj<PantryItemService>('PantryItemService', ['create']);
    toaster = jasmine.createSpyObj<ToasterService>('ToasterService', ['success']);
  });

  it('muestra el estado de carga mientras la respuesta está pendiente', () => {
    externalProductService.getByBarcode.and.returnValue(NEVER);
    setup();

    expect(component.state()).toBe('loading');
    expect(query('product-detail-loading')).not.toBeNull();
    expect(query('product-detail-not-found')).toBeNull();
  });

  it('muestra el detalle de RF-07 y traduce los alérgenos conocidos', () => {
    externalProductService.getByBarcode.and.returnValue(of(milk));
    setup();

    expect(externalProductService.getByBarcode).toHaveBeenCalledWith({ barcode }, { skipHandleError: true });
    const detail = query('product-detail')!.textContent!;
    expect(detail).toContain('Leche entera');
    expect(detail).toContain('Lácteos');
    expect(query('product-allergens')?.textContent).toContain('::Allergen:milk');
    expect(query('product-allergens')?.textContent).toContain('other thing'); // etiqueta desconocida: texto original
    expect(query('product-nutriscore')?.textContent).toContain('B');
    expect(query('product-nova')?.textContent).toContain('::Product:Nova:1');
  });

  it('comunica los datos faltantes sin inventar valores (RF-09)', () => {
    externalProductService.getByBarcode.and.returnValue(
      of({ status: ExternalProductLookupStatus.Found, barcode, name: null, allergens: [] }),
    );
    setup();

    const detail = query('product-detail')!.textContent!;
    expect(detail).toContain('::Product:NoName');
    expect(detail).toContain('::Product:NoBrand');
    expect(detail).toContain('::Product:NoImage');
    expect(query('product-allergens')?.textContent).toContain('::Product:AllergensNotProvided');
    expect(query('product-nutriscore')?.textContent).toContain('::Product:NotProvided');
  });

  it('muestra "no encontrado" y el error con reintento', () => {
    externalProductService.getByBarcode.and.returnValue(of({ status: ExternalProductLookupStatus.NotFound, barcode }));
    setup();
    expect(query('product-detail-not-found')).not.toBeNull();

    externalProductService.getByBarcode.and.returnValue(throwError(() => new Error('BackEnd detenido')));
    component.load();
    fixture.detectChanges();
    expect(query('product-detail-error')).not.toBeNull();
  });

  // ---------- RF-12: comparación con las preferencias al abrir el detalle ----------

  it('muestra la advertencia con cada coincidencia y la leyenda de que no es un diagnóstico', () => {
    externalProductService.getByBarcode.and.returnValue(
      of({
        ...milk,
        preferences: {
          status: PreferenceEvaluationStatus.Match,
          matchedAllergens: ['en:milk'],
          matchedIngredients: ['azúcar'],
          missingAllergens: false,
          missingIngredients: false,
        },
      }),
    );
    setup();

    const box = query('product-preferences-match')!;
    expect(box.textContent).toContain('::Warning:Match');
    expect(box.textContent).toContain('::Allergen:milk');
    expect(box.textContent).toContain('azúcar');
    expect(box.textContent).toContain('::Warning:Disclaimer');
    expect(query('product-preferences-not-verifiable')).toBeNull();
  });

  it('muestra "no verificable" con el dato que falta', () => {
    externalProductService.getByBarcode.and.returnValue(
      of({
        ...milk,
        ingredients: null,
        preferences: {
          status: PreferenceEvaluationStatus.NotVerifiable,
          matchedAllergens: [],
          matchedIngredients: [],
          missingAllergens: false,
          missingIngredients: true,
        },
      }),
    );
    setup();

    const box = query('product-preferences-not-verifiable')!;
    expect(box.textContent).toContain('::Warning:NotVerifiable');
    expect(box.textContent).toContain('::Warning:MissingIngredients');
    expect(box.textContent).not.toContain('::Warning:MissingAllergens');
    expect(query('product-preferences-match')).toBeNull();
  });

  it('sin coincidencias no dice "apto": aclara que es según los datos disponibles', () => {
    externalProductService.getByBarcode.and.returnValue(
      of({ ...milk, preferences: { status: PreferenceEvaluationStatus.NoMatch } }),
    );
    setup();

    expect(query('product-preferences-no-match')?.textContent).toContain('::Warning:NoMatch');
  });

  it('ofrece configurar las preferencias si el usuario no cargó ninguna', () => {
    externalProductService.getByBarcode.and.returnValue(
      of({ ...milk, preferences: { status: PreferenceEvaluationStatus.NoPreferences } }),
    );
    setup();

    const link = query('product-preferences-none')!.querySelector('a')!;
    expect(link.getAttribute('href')).toBe('/preferences');
  });

  it('no muestra nada si el usuario apagó "Mostrar alertas"', () => {
    // El servidor no envía la evaluación cuando las alertas están apagadas.
    externalProductService.getByBarcode.and.returnValue(of({ ...milk, preferences: null }));
    setup();

    expect(query('product-detail')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[data-test^="product-preferences"]')).toBeNull();
  });

  it('no consulta al servidor si el código no tiene un formato válido', () => {
    setup('abc');

    expect(component.state()).toBe('notFound');
    expect(externalProductService.getByBarcode).not.toHaveBeenCalled();
  });

  it('guarda el producto y lo agrega a la despensa con los datos ingresados', () => {
    externalProductService.getByBarcode.and.returnValue(of(milk));
    productService.saveFromExternal.and.returnValue(of({ id: 'product-1', name: 'Leche entera', allergens: [] }));
    pantryItemService.create.and.returnValue(
      of({ id: 'item-1', productId: 'product-1', productName: 'Leche entera', status: PantryItemStatus.Active }),
    );
    setup();

    component.openAddToPantry();
    component.form.patchValue({ quantity: 2, unit: ' l ', expirationDate: { year: 2026, month: 12, day: 5 } });
    component.addToPantry();

    expect(productService.saveFromExternal).toHaveBeenCalledWith({ barcode, name: null });
    expect(pantryItemService.create).toHaveBeenCalledWith({
      productId: 'product-1',
      quantity: 2,
      unit: 'l',
      expirationDate: '2026-12-05',
    });
    expect(component.isModalOpen()).toBeFalse();
    expect(component.addedToPantry()).toBeTrue();
    expect(toaster.success).toHaveBeenCalledWith('::Product:AddedToPantry');
  });

  it('pide el nombre si el catálogo no lo informa y no llama al servidor sin él', () => {
    externalProductService.getByBarcode.and.returnValue(
      of({ status: ExternalProductLookupStatus.Found, barcode, name: null, allergens: [] }),
    );
    productService.saveFromExternal.and.returnValue(of({ id: 'product-1', name: 'Leche suelta', allergens: [] }));
    pantryItemService.create.and.returnValue(of({ id: 'item-1' }));
    setup();

    component.openAddToPantry();
    component.addToPantry();
    expect(component.form.controls.name.hasError('required')).toBeTrue();
    expect(productService.saveFromExternal).not.toHaveBeenCalled();

    component.form.patchValue({ name: ' Leche suelta ' });
    component.addToPantry();
    expect(productService.saveFromExternal).toHaveBeenCalledWith({ barcode, name: 'Leche suelta' });
  });

  it('no llama al servidor con cantidad negativa o fecha inválida', () => {
    externalProductService.getByBarcode.and.returnValue(of(milk));
    setup();

    component.openAddToPantry();
    component.form.patchValue({ quantity: -1, expirationDate: { year: 2026, month: 2, day: 31 } });
    component.addToPantry();

    expect(component.form.controls.quantity.hasError('min')).toBeTrue();
    expect(component.form.controls.expirationDate.hasError('ngbDate')).toBeTrue();
    expect(productService.saveFromExternal).not.toHaveBeenCalled();
  });

  it('mantiene el modal abierto con lo ingresado si el servidor rechaza la operación', () => {
    externalProductService.getByBarcode.and.returnValue(of(milk));
    productService.saveFromExternal.and.returnValue(throwError(() => new Error('503')));
    setup();

    component.openAddToPantry();
    component.form.patchValue({ quantity: 3 });
    component.addToPantry();

    expect(pantryItemService.create).not.toHaveBeenCalled();
    expect(component.isModalOpen()).toBeTrue();
    expect(component.isSaving()).toBeFalse();
    expect(component.form.controls.quantity.value).toBe(3);
  });
});
