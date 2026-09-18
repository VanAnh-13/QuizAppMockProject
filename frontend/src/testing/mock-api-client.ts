import {Injectable} from '@angular/core';

@Injectable()
export class MockApiClient {
    readonly get = vi.fn();
    readonly post = vi.fn();
    readonly put = vi.fn();
    readonly patch = vi.fn();
    readonly list = vi.fn();

    reset(): void {
        this.get.mockReset();
        this.post.mockReset();
        this.put.mockReset();
        this.patch.mockReset();
        this.list.mockReset();
    }
}
