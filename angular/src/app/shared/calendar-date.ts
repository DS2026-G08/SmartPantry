import { AbstractControl, ValidationErrors } from '@angular/forms';
import { NgbDateStruct } from '@ng-bootstrap/ng-bootstrap';

// Las fechas de vencimiento son días calendario "yyyy-MM-dd" sin hora (DateOnly en el BackEnd).
// Se convierten sin pasar por Date para que la zona horaria no corra el día.

/**
 * Fecha opcional; si se informa, tiene que ser un día real. Usa la clave "ngbDate" para que
 * ABP muestre su mensaje de validación localizado, el mismo que usa ngbDatepicker.
 */
export function validDateValidator(control: AbstractControl): ValidationErrors | null {
  const value = control.value as NgbDateStruct | string | null;
  if (value === null || value === '') {
    return null;
  }

  if (typeof value === 'object') {
    const { year, month, day } = value;
    const date = new Date(year, month - 1, day);
    if (date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day) {
      return null;
    }
  }

  return { ngbDate: { invalid: value } };
}

export function toDateStruct(value?: string | null): NgbDateStruct | null {
  if (!value) {
    return null;
  }

  const [year, month, day] = value.substring(0, 10).split('-').map(Number);
  return { year, month, day };
}

export function toIsoDate(value: NgbDateStruct | null): string | null {
  if (!value) {
    return null;
  }

  const pad = (n: number) => String(n).padStart(2, '0');
  return `${value.year}-${pad(value.month)}-${pad(value.day)}`;
}
