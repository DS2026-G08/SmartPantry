import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { catchError, EMPTY, finalize } from 'rxjs';
import { LocalizationPipe } from '@abp/ng.core';
import { ButtonComponent, ToasterService } from '@abp/ng.theme.shared';
import { DietaryPreferenceDto, DietaryPreferenceService } from '../proxy/preferences';
import { KNOWN_ALLERGENS } from '../shared/catalog';
import {
  allergenTag,
  MAX_INGREDIENT_LENGTH,
  MAX_WATCHED_INGREDIENTS,
  MIN_INGREDIENT_LENGTH,
  normalizeIngredient,
} from '../shared/preference-warning';

export type PreferencesLoadState = 'loading' | 'error' | 'loaded';
export type IngredientError = 'invalid' | 'duplicate' | 'tooMany';

// Guardar usa el manejo de errores predeterminado de ABP, que muestra el mensaje del servidor;
// acá sólo se evita que el error quede como "no manejado".
const ignoreHandledError = () => undefined;

/**
 * RF-03: alérgenos e ingredientes a vigilar y si se muestran las alertas. La pantalla repite las reglas
 * del servidor por usabilidad (una palabra inválida no se agrega a la lista); el servidor sigue validando.
 */
@Component({
  selector: 'app-preferences',
  templateUrl: './preferences.component.html',
  styleUrl: './preferences.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonComponent, LocalizationPipe],
})
export class PreferencesComponent {
  private readonly preferenceService = inject(DietaryPreferenceService);
  private readonly toaster = inject(ToasterService);

  readonly knownAllergens = KNOWN_ALLERGENS;
  readonly maxIngredientLength = MAX_INGREDIENT_LENGTH;

  readonly state = signal<PreferencesLoadState>('loading');
  readonly isSaving = signal(false);

  readonly selectedAllergens = signal<ReadonlySet<string>>(new Set());
  readonly ingredients = signal<string[]>([]);
  readonly showAlerts = signal(true);

  readonly newIngredient = signal('');
  readonly ingredientError = signal<IngredientError | null>(null);
  readonly isFull = computed(() => this.ingredients().length >= MAX_WATCHED_INGREDIENTS);

  constructor() {
    this.load();
  }

  load() {
    this.state.set('loading');
    // skipHandleError: la pantalla muestra su propio estado de error con "Reintentar".
    this.preferenceService
      .get({ skipHandleError: true })
      .pipe(
        catchError(() => {
          this.state.set('error');
          return EMPTY;
        }),
      )
      .subscribe(preferences => {
        this.apply(preferences);
        this.state.set('loaded');
      });
  }

  isSelected(allergen: string): boolean {
    return this.selectedAllergens().has(allergenTag(allergen));
  }

  toggleAllergen(allergen: string, checked: boolean) {
    const next = new Set(this.selectedAllergens());
    if (checked) {
      next.add(allergenTag(allergen));
    } else {
      next.delete(allergenTag(allergen));
    }
    this.selectedAllergens.set(next);
  }

  setNewIngredient(value: string) {
    this.newIngredient.set(value);
    this.ingredientError.set(null);
  }

  /** Agrega la palabra sólo si cumple las mismas reglas que el servidor; si no, muestra por qué. */
  addIngredient() {
    const word = this.newIngredient().trim().replace(/\s+/g, ' ');
    const error = this.validate(word);
    this.ingredientError.set(error);
    if (error) {
      return;
    }

    this.ingredients.set([...this.ingredients(), word]);
    this.newIngredient.set('');
  }

  removeIngredient(word: string) {
    this.ingredients.set(this.ingredients().filter(w => w !== word));
    this.ingredientError.set(null);
  }

  save() {
    if (this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    // Las etiquetas se envían en el orden de la lista de 14, igual que las guarda el servidor.
    this.preferenceService
      .update({
        watchedAllergens: this.knownAllergens.map(allergenTag).filter(tag => this.selectedAllergens().has(tag)),
        watchedIngredients: this.ingredients(),
        showAlerts: this.showAlerts(),
      })
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: saved => {
          this.apply(saved);
          this.toaster.success('::Preferences:Saved');
        },
        error: ignoreHandledError,
      });
  }

  private validate(word: string): IngredientError | null {
    if (word.length < MIN_INGREDIENT_LENGTH || word.length > MAX_INGREDIENT_LENGTH) {
      return 'invalid';
    }

    const normalized = normalizeIngredient(word);
    if (this.ingredients().some(w => normalizeIngredient(w) === normalized)) {
      return 'duplicate';
    }

    return this.isFull() ? 'tooMany' : null;
  }

  private apply(preferences: DietaryPreferenceDto) {
    this.selectedAllergens.set(new Set(preferences.watchedAllergens ?? []));
    this.ingredients.set(preferences.watchedIngredients ?? []);
    this.showAlerts.set(preferences.showAlerts ?? true);
  }
}
