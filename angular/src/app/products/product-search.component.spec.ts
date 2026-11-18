import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { NEVER, of, throwError } from 'rxjs';
import { NgxValidateCoreModule } from '@ngx-validate/core';
import { LocalizationPipe } from '@abp/ng.core';
import { CoreTestingModule, LocalizationPipe as MockLocalizationPipe } from '@abp/ng.core/testing';
import { ThemeSharedTestingModule } from '@abp/ng.theme.shared/testing';
import { ExternalProductLookupStatus, ExternalProductService } from '../proxy/external-products';
import { ProductSearchComponent } from './product-search.component';

// El servicio del proxy se reemplaza por un mock: las pruebas no dependen de la red ni de Open Food Facts.
describe('ProductSearchComponent', () => {
  let fixture: ComponentFixture<ProductSearchComponent>;
  let component: ProductSearchComponent;
  let externalProductService: jasmine.SpyObj<ExternalProductService>;

  function setup() {
    TestBed.configureTestingModule({
      imports: [CoreTestingModule.withConfig(), ThemeSharedTestingModule.withConfig(), ProductSearchComponent],
      providers: [provideRouter([]), { provide: ExternalProductService, useValue: externalProductService }],
    });
    TestBed.overrideComponent(ProductSearchComponent, {
      // Los mensajes visuales de validación dependen de la configuración del tema; acá se verifican los errores del formulario.
      remove: { imports: [LocalizationPipe, NgxValidateCoreModule] },
      add: { imports: [MockLocalizationPipe] },
    });

    fixture = TestBed.createComponent(ProductSearchComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  function query(testId: string): HTMLElement | null {
    return fixture.nativeElement.querySelector(`[data-test="${testId}"]`);
  }

  function searchFor(text: string) {
    component.searchControl.setValue(text);
    component.search();
    fixture.detectChanges();
  }

  beforeEach(() => {
    externalProductService = jasmine.createSpyObj<ExternalProductService>('ExternalProductService', ['getList']);
  });

  it('no consulta al servidor hasta que el usuario busca', () => {
    setup();

    expect(component.state()).toBe('idle');
    expect(query('product-search-idle')).not.toBeNull();
    expect(externalProductService.getList).not.toHaveBeenCalled();
    expect(query('product-source')?.textContent).toContain('::Product:SourceNotice');
  });

  it('muestra el estado de carga y no el de "sin resultados" mientras la respuesta está pendiente', () => {
    externalProductService.getList.and.returnValue(NEVER);
    setup();

    searchFor('leche');

    expect(component.state()).toBe('loading');
    expect(query('product-search-loading')).not.toBeNull();
    expect(query('product-search-empty')).toBeNull();
  });

  it('busca por texto y muestra los resultados, comunicando los datos faltantes', () => {
    externalProductService.getList.and.returnValue(
      of({
        status: ExternalProductLookupStatus.Found,
        totalCount: 2,
        items: [
          { barcode: '7790742363206', name: 'Leche entera', brand: 'La Serenísima', imageUrl: null },
          { barcode: '7790742000001', name: null, brand: null, imageUrl: null },
        ],
      }),
    );
    setup();

    searchFor('  leche  ');

    const args = externalProductService.getList.calls.mostRecent().args;
    expect(args[0]).toEqual(jasmine.objectContaining({ text: 'leche', skipCount: 0 }));
    expect(args[1]).toEqual({ skipHandleError: true });

    const rows = fixture.nativeElement.querySelectorAll('[data-test="product-search-item"]');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('Leche entera');
    expect(rows[1].textContent).toContain('::Product:NoName');
    expect(rows[1].textContent).toContain('::Product:NoBrand');
  });

  it('muestra "sin resultados" cuando no hay coincidencias', () => {
    externalProductService.getList.and.returnValue(
      of({ status: ExternalProductLookupStatus.NotFound, totalCount: 0, items: [] }),
    );
    setup();

    searchFor('xyzxyz');

    expect(query('product-search-empty')?.textContent).toContain('::Product:NoResults');
  });

  it('distingue el límite de consultas del error y permite reintentar', () => {
    externalProductService.getList.and.returnValue(of({ status: ExternalProductLookupStatus.RateLimited }));
    setup();

    searchFor('leche');
    expect(query('product-search-rate-limited')).not.toBeNull();

    externalProductService.getList.and.returnValue(throwError(() => new Error('BackEnd detenido')));
    component.retry();
    fixture.detectChanges();

    expect(component.state()).toBe('error');
    expect(query('product-search-error')?.textContent).toContain('::Product:SearchError');
  });

  it('va directo al detalle si se escribe un código de barras', () => {
    setup();
    const router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);

    searchFor('7790742363206');

    expect(router.navigate).toHaveBeenCalledWith(['/products', '7790742363206']);
    expect(externalProductService.getList).not.toHaveBeenCalled();
  });

  it('no consulta al servidor si el texto es demasiado corto', () => {
    setup();

    searchFor('a');

    expect(component.searchControl.hasError('minlength')).toBeTrue();
    expect(externalProductService.getList).not.toHaveBeenCalled();
  });
});
