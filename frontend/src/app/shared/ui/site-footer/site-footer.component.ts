import {ChangeDetectionStrategy, Component} from '@angular/core';
import {RouterLink, RouterLinkActive} from '@angular/router';
import {BrandComponent} from '../brand/brand.component';
import {SITE_NAVIGATION} from '../../../core/config/site-content';

@Component({
    selector: 'app-site-footer',
    imports: [BrandComponent, RouterLink, RouterLinkActive],
    templateUrl: './site-footer.component.html',
    styleUrl: './site-footer.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SiteFooterComponent {
    protected readonly currentYear = new Date().getFullYear();
    protected readonly navItems = SITE_NAVIGATION;
}
