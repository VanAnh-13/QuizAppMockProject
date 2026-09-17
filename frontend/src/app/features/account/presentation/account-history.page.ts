import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {DatePipe} from '@angular/common';
import {RouterLink} from '@angular/router';
import {SiteFooterComponent} from '../../../shared/ui/site-footer/site-footer.component';
import {SiteHeaderComponent} from '../../../shared/ui/site-header/site-header.component';
import {accountApiProvider} from '../infrastructure/account-api.provider';
import {AccountHistoryStore} from './account-history.store';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [DatePipe, RouterLink, SiteFooterComponent, SiteHeaderComponent],
    providers: [accountApiProvider, AccountHistoryStore],
    selector: 'app-account-history-page',
    styleUrl: './account-history.page.css',
    templateUrl: './account-history.page.html',
})
export class AccountHistoryPage {
    protected readonly store = inject(AccountHistoryStore);
}
