import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
import { NgxValidateCoreModule } from '@ngx-validate/core';
import { catchError, EMPTY, tap } from 'rxjs';
import { ListService, LocalizationPipe } from '@abp/ng.core';
import {
  ExternalProductLookupStatus,
  ExternalProductSearchInputDto,
  ExternalProductService,
  ExternalProductSummaryDto,
} from '../proxy/external-products';
import { BARCODE_PATTERN } from '../shared/catalog';

export type ProductSearchState = 'idle' | 'loading' | 'error' | 'rateLimited' | 'empty' | 'loaded';

// Mismos límites que ExternalProductSearchInputDto en el BackEnd.
export const MIN_SEARCH_LENGTH = 2;
export const MAX_SEARCH_LENGTH = 100;
export const SEARCH_PAGE_SIZE = 12;

@Component({
  selector: 'app-product-search',
  templateUrl: './product-search.component.html',
  styleUrl: './product-search.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, NgxValidateCoreModule, NgbPaginationModule, RouterLink, LocalizationPipe],
  providers: [ListService],
})
export class ProductSearchComponent {
  readonly list = inject<ListService<ExternalProductSearchInputDto>>(ListService);
  private readonly externalProductService = inject(ExternalProductService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  // Un solo campo: nombre, marca, categoría o código. No hace falta conocer códigos para buscar.
  readonly searchControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(MIN_SEARCH_LENGTH), Validators.maxLength(MAX_SEARCH_LENGTH)],
  });

  // Grupo de un solo campo: los mensajes de validación de ABP necesitan un formGroup.
  readonly searchForm = new FormGroup({ text: this.searchControl });

  readonly state = signal<ProductSearchState>('idle');
  readonly items = signal<ExternalProductSummaryDto[]>([]);
  readonly totalCount = signal(0);
  // Texto de la búsqueda en curso. Se guarda en la URL (?q=) para volver desde el detalle sin perderla.
  readonly searchedText = signal('');

  constructor() {
    const initialText = this.route.snapshot.queryParamMap.get('q')?.trim() ?? '';
    if (initialText) {
      this.searchControl.setValue(initialText);
      this.searchedText.set(initialText);
    }

    this.list.maxResultCount = SEARCH_PAGE_SIZE;
    this.list
      .hookToQuery(query => {
        const text = this.searchedText();
        if (!text) {
          this.state.set('idle');
          return EMPTY;
        }

        this.state.set('loading');
        // skipHandleError: la pantalla muestra su propio estado de error con "Reintentar".
        return this.externalProductService
          .getList(
            { text, skipCount: query.skipCount, maxResultCount: query.maxResultCount },
            { skipHandleError: true },
          )
          .pipe(
            // Se lee acá, con su tipo real: ListService expone el resultado como PagedResultDto y no incluye "status".
            tap(result => {
              this.items.set(result.items ?? []);
              this.totalCount.set(result.totalCount ?? 0);
              this.state.set(this.toState(result.status));
            }),
            catchError(() => {
              this.state.set('error');
              return EMPTY;
            }),
          );
      })
      .pipe(takeUntilDestroyed())
      .subscribe();
  }

  search() {
    const text = this.searchControl.value.trim();

    // RF-05: un código de barras va directo al detalle del producto.
    if (BARCODE_PATTERN.test(text)) {
      this.router.navigate(['/products', text]);
      return;
    }

    if (this.searchControl.invalid || text.length < MIN_SEARCH_LENGTH) {
      this.searchControl.markAsTouched();
      return;
    }

    this.searchedText.set(text);
    this.router.navigate([], { relativeTo: this.route, queryParams: { q: text }, replaceUrl: true });
    this.list.page = 0;
    this.list.get();
  }

  retry() {
    this.list.get();
  }

  changePage(page: number) {
    this.list.page = page - 1; // ngb-pagination empieza en 1, ListService en 0
  }

  private toState(status?: ExternalProductLookupStatus): ProductSearchState {
    switch (status) {
      case ExternalProductLookupStatus.Found:
        return 'loaded';
      case ExternalProductLookupStatus.NotFound:
        return 'empty';
      case ExternalProductLookupStatus.RateLimited:
        return 'rateLimited';
      default:
        return 'error';
    }
  }
}
