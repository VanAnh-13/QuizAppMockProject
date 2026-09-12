import { ChangeDetectionStrategy, Component } from '@angular/core';
import { BrandComponent } from '../brand/brand.component';

interface NavigationItem {
  readonly href: string;
  readonly isActive?: boolean;
  readonly label: string;
}

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BrandComponent],
  selector: 'app-site-header',
  styleUrl: './site-header.component.css',
  templateUrl: './site-header.component.html',
})
export class SiteHeaderComponent {
  protected readonly navigation: readonly NavigationItem[] = [
    { href: '#quiz-list', isActive: true, label: 'Khám phá quiz' },
    { href: '#gioi-thieu', label: 'Giới thiệu' },
    { href: '#lien-he', label: 'Liên hệ' },
  ];
}
