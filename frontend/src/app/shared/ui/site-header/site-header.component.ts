import { ChangeDetectionStrategy, Component, inject, output } from '@angular/core';
import { BrandComponent } from '../brand/brand.component';
import { AuthDialogComponent } from '../auth-dialog/auth-dialog.component';
import { AuthSession } from '../../../core/auth/auth-session';
import { SiteInfoDialogComponent } from '../site-info-dialog/site-info-dialog.component';

@Component({
  selector: 'app-site-header',
  imports: [BrandComponent, AuthDialogComponent, SiteInfoDialogComponent],
  styleUrl: './site-header.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './site-header.component.html',
})
export class SiteHeaderComponent {
  protected readonly session = inject(AuthSession);
  readonly sessionChanged = output<void>();

  protected logout(): void {
    this.session.clear();
    this.sessionChanged.emit();
  }
}
