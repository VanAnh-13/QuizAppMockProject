import {ChangeDetectionStrategy, Component} from '@angular/core';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    selector: 'app-learning-illustration',
    templateUrl: './learning-illustration.component.html',
    styleUrl: './learning-illustration.component.css',
})
export class LearningIllustrationComponent {
}
