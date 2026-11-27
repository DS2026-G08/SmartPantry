import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NEVER, of, Subject, throwError } from 'rxjs';
import { LocalizationPipe } from '@abp/ng.core';
import { CoreTestingModule, LocalizationPipe as MockLocalizationPipe } from '@abp/ng.core/testing';
import { ToasterService } from '@abp/ng.theme.shared';
import { ThemeSharedTestingModule } from '@abp/ng.theme.shared/testing';
import { DietaryPreferenceDto, DietaryPreferenceService } from '../proxy/preferences';
import { PreferencesComponent } from './preferences.component';

// El servicio del proxy se reemplaza por un mock: las pruebas no dependen de la red.
describe('PreferencesComponent', () => {
  let fixture: ComponentFixture<PreferencesComponent>;
  let component: PreferencesComponent;
  let preferenceService: jasmine.SpyObj<DietaryPreferenceService>;
  let toaster: jasmine.SpyObj<ToasterService>;

  const saved: DietaryPreferenceDto = {
    watchedAllergens: ['en:milk'],
    watchedIngredients: ['azúcar'],
    showAlerts: true,
  };

  function setup() {
    TestBed.configureTestingModule({
      imports: [CoreTestingModule.withConfig(), ThemeSharedTestingModule.withConfig(), PreferencesComponent],
      providers: [
        { provide: DietaryPreferenceService, useValue: preferenceService },
        { provide: ToasterService, useValue: toaster },
      ],
    });
    // El pipe de prueba devuelve la clave: así se verifica qué texto localizado se muestra.
    TestBed.overrideComponent(PreferencesComponent, {
      remove: { imports: [LocalizationPipe] },
      add: { imports: [MockLocalizationPipe] },
    });

    fixture = TestBed.createComponent(PreferencesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  function query(testId: string): HTMLElement | null {
    return fixture.nativeElement.querySelector(`[data-test="${testId}"]`);
  }

  function typeIngredient(text: string) {
    const input = query('preferences-new-ingredient') as HTMLInputElement;
    input.value = text;
    input.dispatchEvent(new Event('input'));
    (query('preferences-add-ingredient') as HTMLButtonElement).click();
    fixture.detectChanges();
  }

  beforeEach(() => {
    preferenceService = jasmine.createSpyObj<DietaryPreferenceService>('DietaryPreferenceService', ['get', 'update']);
    toaster = jasmine.createSpyObj<ToasterService>('ToasterService', ['success']);
  });

  it('muestra la carga y la leyenda de que no es un diagnóstico', () => {
    preferenceService.get.and.returnValue(NEVER);
    setup();

    expect(query('preferences-loading')).not.toBeNull();
    // Regla 1: la leyenda está siempre visible.
    expect(query('preferences-disclaimer')?.textContent).toContain('::Warning:Disclaimer');
  });

  it('carga las preferencias guardadas y maneja su propio error', () => {
    preferenceService.get.and.returnValue(of(saved));
    setup();

    expect(preferenceService.get).toHaveBeenCalledWith({ skipHandleError: true });
    expect(component.isSelected('milk')).toBeTrue();
    expect(component.isSelected('gluten')).toBeFalse();
    expect((fixture.nativeElement.querySelector('#allergen-milk') as HTMLInputElement).checked).toBeTrue();
    expect(query('preferences-ingredients')?.textContent).toContain('azúcar');
    expect(fixture.nativeElement.querySelectorAll('[data-test="preferences-allergens"] input').length).toBe(14);
  });

  it('muestra el error con "Reintentar" y vuelve a consultar', () => {
    preferenceService.get.and.returnValue(throwError(() => new Error('BackEnd detenido')));
    setup();

    expect(component.state()).toBe('error');
    expect(query('preferences-error')).not.toBeNull();

    preferenceService.get.and.returnValue(of(saved));
    (query('preferences-retry') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(component.state()).toBe('loaded');
    expect(preferenceService.get).toHaveBeenCalledTimes(2);
  });

  it('guarda las etiquetas en el orden de la lista, las palabras y el interruptor', () => {
    preferenceService.get.and.returnValue(of(saved));
    const response = new Subject<DietaryPreferenceDto>();
    preferenceService.update.and.returnValue(response);
    setup();

    component.toggleAllergen('gluten', true);
    typeIngredient('  harina   de trigo ');
    component.showAlerts.set(false);
    component.save();

    expect(preferenceService.update).toHaveBeenCalledWith({
      watchedAllergens: ['en:gluten', 'en:milk'],
      watchedIngredients: ['azúcar', 'harina de trigo'],
      showAlerts: false,
    });
    expect(component.isSaving()).toBeTrue();

    response.next({ watchedAllergens: ['en:gluten', 'en:milk'], watchedIngredients: ['azúcar', 'harina de trigo'], showAlerts: false });
    response.complete();

    expect(component.isSaving()).toBeFalse();
    expect(toaster.success).toHaveBeenCalledWith('::Preferences:Saved');
  });

  it('guarda al hacer clic en "Guardar"', () => {
    preferenceService.get.and.returnValue(of(saved));
    preferenceService.update.and.returnValue(of(saved));
    setup();

    (query('preferences-save')!.querySelector('button') as HTMLButtonElement).click();

    expect(preferenceService.update).toHaveBeenCalledTimes(1);
  });

  it('no agrega palabras inválidas ni repetidas y no llama al servidor', () => {
    preferenceService.get.and.returnValue(of(saved));
    setup();

    typeIngredient('a');
    expect(component.ingredientError()).toBe('invalid');
    expect(query('preferences-ingredient-error')?.textContent).toContain('::Preferences:IngredientInvalid');

    typeIngredient('AZUCAR'); // misma palabra sin acento ni minúsculas
    expect(component.ingredientError()).toBe('duplicate');

    expect(component.ingredients()).toEqual(['azúcar']);
    expect(preferenceService.update).not.toHaveBeenCalled();
  });

  it('no deja pasar de 20 palabras', () => {
    preferenceService.get.and.returnValue(
      of({ ...saved, watchedIngredients: Array.from({ length: 20 }, (_, i) => `palabra ${i}`) }),
    );
    setup();

    expect(component.isFull()).toBeTrue();
    expect((query('preferences-add-ingredient') as HTMLButtonElement).disabled).toBeTrue();
  });

  it('quita una palabra de la lista', () => {
    preferenceService.get.and.returnValue(of(saved));
    setup();

    component.removeIngredient('azúcar');
    fixture.detectChanges();

    expect(component.ingredients()).toEqual([]);
    expect(query('preferences-ingredient')).toBeNull();
  });
});
