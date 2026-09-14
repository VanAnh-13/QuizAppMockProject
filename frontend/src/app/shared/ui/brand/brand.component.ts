import {ChangeDetectionStrategy, Component} from '@angular/core';
import {RouterLink} from '@angular/router';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RouterLink],
    selector: 'app-brand',
    styleUrl: './brand.component.css',
    templateUrl: './brand.component.html',
})
export class BrandComponent {
}
