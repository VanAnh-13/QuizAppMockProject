import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {ErrorNotificationService} from '../../../core/errors/error-notification.service';

@Component({
    selector: 'app-toast',
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './toast.component.html',
    styleUrl: './toast.component.css',
})
export class ToastComponent {
    protected readonly service = inject(ErrorNotificationService);
}
