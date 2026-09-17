import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {ModalDirective} from '../dialog/modal.directive';
import {ConfirmationService} from './confirmation.service';

@Component({
    selector: 'app-confirmation-dialog',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [ModalDirective],
    templateUrl: './confirmation-dialog.component.html',
    styleUrl: './confirmation-dialog.component.css',
})
export class ConfirmationDialogComponent {
    protected readonly service = inject(ConfirmationService);
}
