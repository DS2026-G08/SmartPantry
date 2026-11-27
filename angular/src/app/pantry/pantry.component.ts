import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NgbDatepickerModule, NgbDateStruct, NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
import { NgxValidateCoreModule } from '@ngx-validate/core';
import { catchError, EMPTY, finalize } from 'rxjs';
import { ListService, LocalizationPipe } from '@abp/ng.core';
import {
  ButtonComponent,
  Confirmation,
  ConfirmationService,
  ModalCloseDirective,
  ModalComponent,
  ToasterService,
} from '@abp/ng.theme.shared';
import {
  GetPantryItemListDto,
  PantryItemDto,
  PantryItemService,
  PantryItemStatus,
} from '../proxy/pantries';
import { PreferenceEvaluationStatus } from '../proxy/preferences';
import { AllergenLabel } from '../shared/catalog';
import { preferenceMatches } from '../shared/preference-warning';
import { toDateStruct, toIsoDate, validDateValidator } from '../shared/calendar-date';

// Mismo máximo que PantryItemConsts.MaxUnitLength en el BackEnd.
export const MAX_UNIT_LENGTH = 16;

export type PantryLoadState = 'loading' | 'error' | 'loaded';

// Editar, consumir y eliminar usan el manejo de errores predeterminado de ABP, que ya muestra
// un mensaje comprensible; acá sólo se evita que el error quede como "no manejado".
const ignoreHandledError = () => undefined;

@Component({
  selector: 'app-pantry',
  templateUrl: './pantry.component.html',
  styleUrl: './pantry.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    // Muestra debajo de cada campo los mensajes de validación localizados de ABP.
    NgxValidateCoreModule,
    NgbDatepickerModule,
    NgbPaginationModule,
    ModalComponent,
    ModalCloseDirective,
    ButtonComponent,
    LocalizationPipe,
    DatePipe,
    DecimalPipe,
    RouterLink,
  ],
  providers: [ListService],
})
export class PantryComponent {
  readonly list = inject<ListService<GetPantryItemListDto>>(ListService);
  private readonly pantryItemService = inject(PantryItemService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly toaster = inject(ToasterService);
  private readonly fb = inject(FormBuilder);

  readonly statuses = PantryItemStatus;
  readonly preferenceStatuses = PreferenceEvaluationStatus;
  readonly maxUnitLength = MAX_UNIT_LENGTH;

  readonly state = signal<PantryLoadState>('loading');
  readonly items = signal<PantryItemDto[]>([]);
  readonly totalCount = signal(0);
  readonly includeConsumed = signal(false);

  readonly selectedItem = signal<PantryItemDto | undefined>(undefined);
  readonly isModalOpen = signal(false);
  readonly isSaving = signal(false);

  // La interfaz repite las reglas del servidor por usabilidad; el servidor sigue validando.
  readonly form = this.fb.group({
    quantity: this.fb.control<number | null>(null, Validators.min(0)),
    unit: this.fb.control<string | null>(null, Validators.maxLength(MAX_UNIT_LENGTH)),
    expirationDate: this.fb.control<NgbDateStruct | null>(null, validDateValidator),
  });

  constructor() {
    this.list
      .hookToQuery(query => {
        this.state.set('loading');
        // skipHandleError: el componente muestra su propio estado de error con "Reintentar"
        // en lugar del modal global de ABP sobre una pantalla vacía.
        return this.pantryItemService
          .getList(
            {
              skipCount: query.skipCount,
              maxResultCount: query.maxResultCount,
              includeConsumed: this.includeConsumed(),
            },
            { skipHandleError: true },
          )
          .pipe(
            catchError(() => {
              this.state.set('error');
              return EMPTY;
            }),
          );
      })
      .pipe(takeUntilDestroyed())
      .subscribe(result => {
        this.items.set(result.items ?? []);
        this.totalCount.set(result.totalCount ?? 0);
        this.state.set('loaded');
      });
  }

  // RF-12: el servidor sólo envía la advertencia si el usuario tiene encendido "Mostrar alertas".
  warningMatches(item: PantryItemDto): AllergenLabel[] {
    return preferenceMatches(item.preferenceWarning);
  }

  retry() {
    this.list.get();
  }

  toggleConsumed(includeConsumed: boolean) {
    this.includeConsumed.set(includeConsumed);
    this.list.page = 0;
    this.list.get();
  }

  changePage(page: number) {
    this.list.page = page - 1; // ngb-pagination empieza en 1, ListService en 0
  }

  edit(item: PantryItemDto) {
    this.selectedItem.set(item);
    this.form.reset({
      quantity: item.quantity ?? null,
      unit: item.unit ?? null,
      expirationDate: toDateStruct(item.expirationDate),
    });
    this.isModalOpen.set(true);
  }

  save() {
    const item = this.selectedItem();
    if (!item || this.isSaving()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { quantity, unit, expirationDate } = this.form.getRawValue();
    this.isSaving.set(true);

    // Si el servidor rechaza los datos, ABP muestra su mensaje y el modal sigue abierto
    // con lo ingresado: sólo se cierra cuando la actualización termina bien.
    this.pantryItemService
      .update(item.id!, {
        quantity: quantity ?? null,
        unit: unit?.trim() || null,
        expirationDate: toIsoDate(expirationDate),
      })
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: () => {
          this.isModalOpen.set(false);
          this.toaster.success('::Pantry:ItemSaved');
          this.list.get();
        },
        error: ignoreHandledError,
      });
  }

  consume(item: PantryItemDto) {
    this.pantryItemService.consume(item.id!).subscribe({
      next: () => {
        this.toaster.success('::Pantry:ItemConsumed');
        this.list.get();
      },
      error: ignoreHandledError,
    });
  }

  delete(item: PantryItemDto) {
    this.confirmation
      .warn('::Pantry:DeleteConfirmation', '::Pantry:AreYouSure', {
        messageLocalizationParams: [item.productName ?? ''],
      })
      .subscribe(status => {
        if (status !== Confirmation.Status.confirm) {
          return; // cancelado: no se llama al servidor
        }

        this.pantryItemService.delete(item.id!).subscribe({
          next: () => {
            this.toaster.success('::Pantry:ItemDeleted');
            this.list.get();
          },
          error: ignoreHandledError,
        });
      });
  }
}
