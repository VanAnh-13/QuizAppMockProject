import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {ReactiveFormsModule} from '@angular/forms';
import {CONTACT_DETAILS, CONTACT_FAQS} from '../../../core/config/site-content';
import {SiteFooterComponent} from '../../../shared/ui/site-footer/site-footer.component';
import {SiteHeaderComponent} from '../../../shared/ui/site-header/site-header.component';
import {ContactStore} from './contact.store';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [ReactiveFormsModule, SiteFooterComponent, SiteHeaderComponent],
    providers: [ContactStore],
    selector: 'app-contact-page',
    styleUrl: './contact.page.css',
    templateUrl: './contact.page.html',
})
export class ContactPage {
    protected readonly store = inject(ContactStore);
    protected readonly details = CONTACT_DETAILS;
    protected readonly faqs = CONTACT_FAQS;

    protected submit(): void {
        void this.store.submit();
    }
}
