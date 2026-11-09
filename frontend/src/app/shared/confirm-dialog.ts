import { Component, effect, ElementRef, input, output, viewChild } from '@angular/core';

@Component({
  selector: 'app-confirm-dialog',
  template: `
    <dialog
      #dialog
      class="m-auto w-[min(28rem,calc(100%-2rem))] rounded-lg p-0 shadow-xl backdrop:bg-black/40"
      (close)="cancel.emit()"
    >
      <div class="p-6">
        <h2 class="text-lg font-semibold text-gray-900">{{ title() }}</h2>
        <p class="mt-2 text-sm text-gray-600">{{ message() }}</p>
        <div class="mt-6 flex justify-end gap-2">
          <button type="button" class="btn-secondary" (click)="dialog.close()">Cancel</button>
          <button type="button" class="btn-danger" [disabled]="busy()" (click)="confirm.emit()">
            {{ busy() ? 'Deleting…' : confirmLabel() }}
          </button>
        </div>
      </div>
    </dialog>
  `,
})
export class ConfirmDialog {
  readonly open = input(false);
  readonly busy = input(false);
  readonly title = input.required<string>();
  readonly message = input.required<string>();
  readonly confirmLabel = input('Delete');

  readonly confirm = output();
  readonly cancel = output();

  private dialogRef = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  constructor() {
    effect(() => {
      const dialog = this.dialogRef().nativeElement;
      if (this.open() && !dialog.open) {
        dialog.showModal();
      } else if (!this.open() && dialog.open) {
        dialog.close();
      }
    });
  }
}
