import {ChangeDetectionStrategy, Component, input, signal, viewChild} from '@angular/core';
import {SITE_INFORMATION, SiteInformation} from '../../../core/config/site-content';
import {DialogComponent} from '../dialog/dialog.component';

@Component({
    selector: 'app-site-info-dialog',
    imports: [DialogComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './site-info-dialog.component.html',
    styleUrl: './site-info-dialog.component.css',
})
export class SiteInfoDialogComponent {
    readonly dialogId = input.required<string>();
    protected readonly content = signal(SITE_INFORMATION.about);

    private readonly dialog = viewChild.required<DialogComponent>('dialog');

    open(id: SiteInformation): void {
        this.content.set(SITE_INFORMATION[id]);
        this.dialog().open();
    }
}
