import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { NgbDatepickerModule, NgbDateStruct } from '@ng-bootstrap/ng-bootstrap';
import { NgxValidateCoreModule } from '@ngx-validate/core';
import { catchError, EMPTY, finalize, switchMap } from 'rxjs';
import { LocalizationPipe } from '@abp/ng.core';
import { ButtonComponent, ModalCloseDirective, ModalComponent, ToasterService } from '@abp/ng.theme.shared';
import {
  ExternalProductLookupResultDto,
  ExternalProductLookupStatus,
  ExternalProductService,
} from '../proxy/external-products';
import { PantryItemService } from '../proxy/pantries';
import { ProductService } from '../proxy/products';
import { MAX_UNIT_LENGTH } from '../pantry/pantry.component';
import { toIsoDate, validDateValidator } from '../shared/calendar-date';
import { allergenLabel, AllergenLabel, BARCODE_PATTERN } from '../shared/catalog';

export type ProductDetailState = 'loading' | 'error' | 'rateLimited' | 'notFound' | 'loaded';

// Mismo máximo que ProductConsts.MaxNameLength en el BackEnd.
export const MAX_PRODUCT_NAME_LENGTH = 128;

// Agregar usa el manejo de errores predeterminado de ABP, que muestra el mensaje del servidor;
// acá sólo se evita que el error quede como "no manejado".
const ignoreHandledError = () => undefined;

@Component({
  selector: 'app-product-detail',
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    NgxValidateCoreModule,
    NgbDatepickerModule,
    RouterLink,
    ModalComponent,
    ModalCloseDirective,
    ButtonComponent,
    LocalizationPipe,
  ],
})
export class ProductDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly externalProductService = inject(ExternalProductService);
  private readonly productService = inject(ProductService);
  private readonly pantryItemService = inject(PantryItemService);
  private readonly toaster = inject(ToasterService);
  private readonly fb = inject(FormBuilder);

  readonly maxUnitLength = MAX_UNIT_LENGTH;
  readonly maxNameLength = MAX_PRODUCT_NAME_LENGTH;

  readonly barcode = signal('');
  // Texto de la búsqueda de la que se vino, para volver a los mismos resultados.
  readonly backQuery = signal<string | null>(null);
  readonly state = signal<ProductDetailState>('loading');
  readonly product = signal<ExternalProductLookupResultDto | undefined>(undefined);
  readonly allergens = signal<AllergenLabel[]>([]);

  readonly isModalOpen = signal(false);
  readonly isSaving = signal(false);
  readonly addedToPantry = signal(false);

  // El nombre sólo se pide si el catálogo no lo informa (RF-09: no se inventa, lo escribe el usuario).
  readonly form = this.fb.group({
    name: this.fb.control<string | null>(null, Validators.maxLength(MAX_PRODUCT_NAME_LENGTH)),
    quantity: this.fb.control<number | null>(null, Validators.min(0)),
    unit: this.fb.control<string | null>(null, Validators.maxLength(MAX_UNIT_LENGTH)),
    expirationDate: this.fb.control<NgbDateStruct | null>(null, validDateValidator),
  });

  constructor() {
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => this.backQuery.set(params.get('q')));
    this.route.paramMap.pipe(takeUntilDestroyed()).subscribe(params => {
      this.barcode.set(params.get('barcode')?.trim() ?? '');
      this.load();
    });
  }

  get needsName(): boolean {
    return !this.product()?.name;
  }

  load() {
    const barcode = this.barcode();
    this.addedToPantry.set(false);

    // Un código con formato inválido no puede existir: no se consulta al servidor.
    if (!BARCODE_PATTERN.test(barcode)) {
      this.state.set('notFound');
      return;
    }

    this.state.set('loading');
    // skipHandleError: la pantalla muestra su propio estado de error con "Reintentar".
    this.externalProductService
      .getByBarcode({ barcode }, { skipHandleError: true })
      .pipe(
        catchError(() => {
          this.state.set('error');
          return EMPTY;
        }),
      )
      .subscribe(result => {
        this.product.set(result);
        this.allergens.set((result.allergens ?? []).map(allergenLabel));
        this.state.set(this.toState(result.status));
      });
  }

  openAddToPantry() {
    this.form.reset({ name: null, quantity: null, unit: null, expirationDate: null });
    const name = this.form.controls.name;
    name.setValidators(
      this.needsName
        ? [Validators.required, Validators.maxLength(MAX_PRODUCT_NAME_LENGTH)]
        : Validators.maxLength(MAX_PRODUCT_NAME_LENGTH),
    );
    name.updateValueAndValidity();
    this.isModalOpen.set(true);
  }

  addToPantry() {
    if (this.isSaving()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, quantity, unit, expirationDate } = this.form.getRawValue();
    this.isSaving.set(true);

    // RF-08 y RF-10: primero se guarda el producto (sin duplicar el código) y después se agrega a la despensa.
    // Si el servidor rechaza los datos, ABP muestra su mensaje y el modal sigue abierto con lo ingresado.
    this.productService
      .saveFromExternal({ barcode: this.barcode(), name: this.needsName ? name?.trim() || null : null })
      .pipe(
        switchMap(saved =>
          this.pantryItemService.create({
            productId: saved.id!,
            quantity: quantity ?? null,
            unit: unit?.trim() || null,
            expirationDate: toIsoDate(expirationDate),
          }),
        ),
        finalize(() => this.isSaving.set(false)),
      )
      .subscribe({
        next: () => {
          this.isModalOpen.set(false);
          this.addedToPantry.set(true);
          this.toaster.success('::Product:AddedToPantry');
        },
        error: ignoreHandledError,
      });
  }

  nutriScoreClass(grade: string): string {
    switch (grade) {
      case 'a':
      case 'b':
        return 'bg-success';
      case 'c':
        return 'bg-warning text-dark';
      default:
        return 'bg-danger';
    }
  }

  private toState(status?: ExternalProductLookupStatus): ProductDetailState {
    switch (status) {
      case ExternalProductLookupStatus.Found:
        return 'loaded';
      case ExternalProductLookupStatus.NotFound:
        return 'notFound';
      case ExternalProductLookupStatus.RateLimited:
        return 'rateLimited';
      default:
        return 'error';
    }
  }
}
