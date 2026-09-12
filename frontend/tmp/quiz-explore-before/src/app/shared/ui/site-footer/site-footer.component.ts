import { ChangeDetectionStrategy, Component } from '@angular/core';
import { BrandComponent } from '../brand/brand.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BrandComponent],
  selector: 'app-site-footer',
  styleUrl: './site-footer.component.css',
  templateUrl: './site-footer.component.html',
})
export class SiteFooterComponent {
  protected readonly currentYear = new Date().getFullYear();
}
