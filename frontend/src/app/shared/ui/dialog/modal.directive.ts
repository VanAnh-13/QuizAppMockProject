import {AfterViewInit, Directive, ElementRef, inject, OnDestroy, output} from '@angular/core';

@Directive({
    selector: 'dialog[appModal]',
    host: {
        '(cancel)': 'modalDismissed.emit()',
        '(click)': 'dismissBackdrop($event)',
    },
})
export class ModalDirective implements AfterViewInit, OnDestroy {
    readonly modalDismissed = output<void>();
    private readonly dialog = inject<ElementRef<HTMLDialogElement>>(ElementRef).nativeElement;

    ngAfterViewInit(): void {
        this.dialog.showModal();
    }

    ngOnDestroy(): void {
        if (this.dialog.open) this.dialog.close();
    }

    protected dismissBackdrop(event: MouseEvent): void {
        if (event.target !== this.dialog) return;
        const bounds = this.dialog.getBoundingClientRect();
        if (event.clientX < bounds.left || event.clientX > bounds.right ||
            event.clientY < bounds.top || event.clientY > bounds.bottom) {
            this.modalDismissed.emit();
        }
    }
}
