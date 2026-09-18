import {ChangeDetectionStrategy, Component, computed, input, output} from '@angular/core';
import {AsyncState} from '../../state/async-state';

@Component({
    selector: 'app-async-content',
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './async-content.component.html',
    styleUrl: './async-content.component.css',
})
export class AsyncContentComponent<T = unknown> {
    readonly state = input.required<AsyncState<T>>();
    readonly loadingMessage = input('Loading…');
    readonly retryRequested = output<void>();
    protected readonly errorMessage = computed(() => {
        const current = this.state();
        return current.status === 'error' ? current.error : '';
    });
}
