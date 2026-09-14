import {ChangeDetectionStrategy, Component} from '@angular/core';
import {BrandComponent} from '../brand/brand.component';
import {SiteInfoDialogComponent} from '../site-info-dialog/site-info-dialog.component';
import {SITE_NAVIGATION} from '../../../core/config/site-content';

@Component({
    selector: 'app-site-footer',
    imports: [BrandComponent, SiteInfoDialogComponent],
    templateUrl: './site-footer.component.html',
    styleUrl: './site-footer.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SiteFooterComponent {
    protected readonly currentYear = new Date().getFullYear();
    protected readonly navigation = SITE_NAVIGATION;
}
