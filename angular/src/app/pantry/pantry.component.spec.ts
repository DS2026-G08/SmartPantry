import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NEVER, of, throwError } from 'rxjs';
import { LocalizationPipe } from '@abp/ng.core';
import { CoreTestingModule, LocalizationPipe as MockLocalizationPipe } from '@abp/ng.core/testing';
import { Confirmation, ConfirmationService, ToasterService } from '@abp/ng.theme.shared';
import { ThemeSharedTestingModule } from '@abp/ng.theme.shared/testing';
import { PantryItemDto, PantryItemService, PantryItemStatus } from '../proxy/pantries';
import { PantryComponent } from './pantry.component';

// El servicio del proxy se reemplaza por un mock: las pruebas no dependen de la red.
describe('PantryComponent', () => {
  let fixture: ComponentFixture<PantryComponent>;
  let component: PantryComponent;
  let pantryItemService: jasmine.SpyObj<PantryItemService>;
  let confirmation: jasmine.SpyObj<ConfirmationService>;
  let toaster: jasmine.SpyObj<ToasterService>;

  const milk: PantryItemDto = {
    id: 'item-1',
    productName: 'Leche',
    productBrand: 'La Serenisima',
    status: PantryItemStatus.Active,
    expirationDate: '2026-11-10',
    quantity: 2,
    unit: 'l',
  };

  const rice: PantryItemDto = {
    id: 'item-2',
    productName: 'Arroz',
    productBrand: 'Gallo',
    status: PantryItemStatus.Active,
    expirationDate: null,
    quantity: null,
    unit: null,
  };

  function setup() {
    TestBed.configureTestingModule({
      imports: [CoreTestingModule.withConfig(), ThemeSharedTestingModule.withConfig(), PantryComponent],
      providers: [
        { provide: PantryItemService, useValue: pantryItemService },
        { provide: ConfirmationService, useValue: confirmation },
        { provide: ToasterService, useValue: toaster },
      ],
    });
    // El pipe de prueba devuelve la clave: así se verifica qué texto localizado se muestra.
    TestBed.overrideComponent(PantryComponent, {
      remove: { imports: [LocalizationPipe] },
      add: { imports: [MockLocalizationPipe] },
    });

    fixture = TestBed.createComponent(PantryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  function query(testId: string): HTMLElement | null {
    return fixture.nativeElement.querySelector(`[data-test="${testId}"]`);
  }

  beforeEach(() => {
    pantryItemService = jasmine.createSpyObj<PantryItemService>('PantryItemService', [
      'getList',
      'update',
      'consume',
      'delete',
    ]);
    confirmation = jasmine.createSpyObj<ConfirmationService>('ConfirmationService', ['warn']);
    toaster = jasmine.createSpyObj<ToasterService>('ToasterService', ['success']);
  });

  it('muestra el estado de carga y no el de lista vacía mientras la respuesta está pendiente', () => {
    pantryItemService.getList.and.returnValue(NEVER);
    setup();

    expect(component.state()).toBe('loading');
    expect(query('pantry-loading')).not.toBeNull();
    expect(query('pantry-empty')).toBeNull();
  });

  it('muestra el estado de error y permite reintentar', () => {
    pantryItemService.getList.and.returnValue(throwError(() => new Error('BackEnd detenido')));
    setup();

    expect(component.state()).toBe('error');
    expect(query('pantry-error')?.textContent).toContain('::Pantry:LoadError');
    // El listado maneja su propio error en lugar del modal global de ABP.
    expect(pantryItemService.getList.calls.mostRecent().args[1]).toEqual({ skipHandleError: true });

    pantryItemService.getList.and.returnValue(of({ items: [milk], totalCount: 1 }));
    (query('pantry-error')?.querySelector('button') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(pantryItemService.getList).toHaveBeenCalledTimes(2);
    expect(component.state()).toBe('loaded');
    expect(query('pantry-items')).not.toBeNull();
  });

  it('muestra la lista vacía con el mensaje localizado', () => {
    pantryItemService.getList.and.returnValue(of({ items: [], totalCount: 0 }));
    setup();

    expect(query('pantry-empty')?.textContent).toContain('::Pantry:Empty');
    expect(query('pantry-items')).toBeNull();
  });

  it('muestra los ítems y comunica los datos faltantes sin inventar valores', () => {
    pantryItemService.getList.and.returnValue(of({ items: [milk, rice], totalCount: 2 }));
    setup();

    const rows = fixture.nativeElement.querySelectorAll('[data-test="pantry-item"]');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('Leche');
    expect(rows[0].textContent).toContain('l');
    expect(rows[1].textContent).toContain('Arroz');
    expect(rows[1].textContent).toContain('::Pantry:NoExpirationDate');
    expect(rows[1].textContent).toContain('::Pantry:NoQuantity');
  });

  it('distingue los consumidos y los pide al activar el filtro', () => {
    pantryItemService.getList.and.returnValue(of({ items: [milk], totalCount: 1 }));
    setup();

    pantryItemService.getList.and.returnValue(
      of({ items: [{ ...milk, status: PantryItemStatus.Consumed }], totalCount: 1 }),
    );
    component.toggleConsumed(true);
    fixture.detectChanges();

    expect(pantryItemService.getList.calls.mostRecent().args[0].includeConsumed).toBeTrue();
    expect(query('pantry-consumed-badge')?.textContent).toContain('::Pantry:Status:Consumed');
  });

  it('no llama al servidor si la edición tiene cantidad negativa', () => {
    pantryItemService.getList.and.returnValue(of({ items: [milk], totalCount: 1 }));
    setup();

    component.edit(milk);
    component.form.controls.quantity.setValue(-1);
    component.save();

    expect(component.form.controls.quantity.hasError('min')).toBeTrue();
    expect(pantryItemService.update).not.toHaveBeenCalled();
  });

  it('no llama al servidor si la fecha no es válida', () => {
    pantryItemService.getList.and.returnValue(of({ items: [milk], totalCount: 1 }));
    setup();

    component.edit(milk);
    component.form.controls.expirationDate.setValue({ year: 2026, month: 2, day: 31 });
    component.save();

    expect(component.form.controls.expirationDate.hasError('ngbDate')).toBeTrue();
    expect(pantryItemService.update).not.toHaveBeenCalled();
  });

  it('precarga el formulario y guarda los datos válidos con el proxy', () => {
    pantryItemService.getList.and.returnValue(of({ items: [milk], totalCount: 1 }));
    pantryItemService.update.and.returnValue(of({ ...milk, quantity: 1.5 }));
    setup();

    component.edit(milk);
    expect(component.form.getRawValue()).toEqual({
      quantity: 2,
      unit: 'l',
      expirationDate: { year: 2026, month: 11, day: 10 },
    });

    component.form.patchValue({ quantity: 1.5, unit: '  l  ', expirationDate: { year: 2026, month: 11, day: 5 } });
    component.save();

    expect(pantryItemService.update).toHaveBeenCalledWith('item-1', {
      quantity: 1.5,
      unit: 'l',
      expirationDate: '2026-11-05',
    });
    expect(component.isModalOpen()).toBeFalse();
    expect(component.isSaving()).toBeFalse();
    expect(pantryItemService.getList).toHaveBeenCalledTimes(2); // la lista se actualiza
  });

  it('mantiene el modal abierto con lo ingresado si el servidor rechaza los datos', () => {
    pantryItemService.getList.and.returnValue(of({ items: [milk], totalCount: 1 }));
    pantryItemService.update.and.returnValue(throwError(() => new Error('400')));
    setup();

    component.edit(milk);
    component.form.patchValue({ quantity: 3 });
    component.save();

    expect(component.isModalOpen()).toBeTrue();
    expect(component.isSaving()).toBeFalse();
    expect(component.form.controls.quantity.value).toBe(3);
  });

  it('pide confirmación antes de eliminar y no llama al servidor si se cancela', () => {
    pantryItemService.getList.and.returnValue(of({ items: [milk], totalCount: 1 }));
    confirmation.warn.and.returnValue(of(Confirmation.Status.reject));
    setup();

    component.delete(milk);

    expect(confirmation.warn).toHaveBeenCalledWith('::Pantry:DeleteConfirmation', '::Pantry:AreYouSure', {
      messageLocalizationParams: ['Leche'],
    });
    expect(pantryItemService.delete).not.toHaveBeenCalled();
  });

  it('elimina el ítem al confirmar y actualiza la lista', () => {
    pantryItemService.getList.and.returnValue(of({ items: [milk], totalCount: 1 }));
    pantryItemService.delete.and.returnValue(of(undefined));
    confirmation.warn.and.returnValue(of(Confirmation.Status.confirm));
    setup();

    component.delete(milk);

    expect(pantryItemService.delete).toHaveBeenCalledWith('item-1');
    expect(pantryItemService.getList).toHaveBeenCalledTimes(2);
  });

  it('marca un ítem como consumido con el proxy', () => {
    pantryItemService.getList.and.returnValue(of({ items: [milk], totalCount: 1 }));
    pantryItemService.consume.and.returnValue(of({ ...milk, status: PantryItemStatus.Consumed }));
    setup();

    component.consume(milk);

    expect(pantryItemService.consume).toHaveBeenCalledWith('item-1');
    expect(toaster.success).toHaveBeenCalledWith('::Pantry:ItemConsumed');
  });
});
