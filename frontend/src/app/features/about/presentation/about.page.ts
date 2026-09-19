import {ChangeDetectionStrategy, Component} from '@angular/core';
import {RouterLink} from '@angular/router';
import {SiteFooterComponent} from '../../../shared/ui/site-footer/site-footer.component';
import {SiteHeaderComponent} from '../../../shared/ui/site-header/site-header.component';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RouterLink, SiteFooterComponent, SiteHeaderComponent],
    selector: 'app-about-page',
    styleUrl: './about.page.css',
    templateUrl: './about.page.html',
})
export class AboutPage {
}
