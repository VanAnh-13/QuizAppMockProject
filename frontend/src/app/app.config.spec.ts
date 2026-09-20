import {ViewportScroller} from '@angular/common';
import {TestBed} from '@angular/core/testing';
import {Router} from '@angular/router';
import {RouterTestingHarness} from '@angular/router/testing';
import {appConfig} from './app.config';

describe('Route transitions', () => {
    const originalTransition = Object.getOwnPropertyDescriptor(document, 'startViewTransition');

    afterEach(() => {
        if (originalTransition) {
            Object.defineProperty(document, 'startViewTransition', originalTransition);
        } else {
            Reflect.deleteProperty(document, 'startViewTransition');
        }
        vi.unstubAllGlobals();
    });

    async function setup(supported = true) {
        vi.stubGlobal('matchMedia', vi.fn().mockImplementation(() => ({matches: false})));
        const skipTransition = vi.fn();
        const startViewTransition = vi.fn((update: () => Promise<void>) => {
            const updateCallbackDone = Promise.resolve().then(update);
            return {
                ready: updateCallbackDone,
                finished: updateCallbackDone,
                updateCallbackDone,
                skipTransition,
            };
        });
        Object.defineProperty(document, 'startViewTransition', {
            configurable: true,
            value: supported ? startViewTransition : undefined,
        });
        TestBed.configureTestingModule({
            providers: [...appConfig.providers],
        });
        vi.spyOn(TestBed.inject(ViewportScroller), 'scrollToPosition').mockImplementation(() => {});
        const harness = await RouterTestingHarness.create('/about');
        return {harness, startViewTransition};
    }

    it('navigates between public pages without capturing or animating the full document', async () => {
        const {harness, startViewTransition} = await setup();
        expect(startViewTransition).not.toHaveBeenCalled();

        for (const url of ['/contact', '/login', '/register', '/about']) {
            await harness.navigateByUrl(url);

            expect(startViewTransition).not.toHaveBeenCalled();
            expect(TestBed.inject(Router).url).toBe(url);
            expect(harness.routeNativeElement?.querySelector('h1')).not.toBeNull();
        }
    });

    it.each(['/about?filter=active', '/about#main-content'])(
        'updates %s without a document snapshot', async (url) => {
            const {harness, startViewTransition} = await setup();

            await harness.navigateByUrl(url);

            expect(startViewTransition).not.toHaveBeenCalled();
            expect(TestBed.inject(Router).url).toBe(url);
        },
    );

    it('navigates when the browser does not support view transitions', async () => {
        const {harness, startViewTransition} = await setup(false);

        await harness.navigateByUrl('/contact');

        expect(startViewTransition).not.toHaveBeenCalled();
        expect(TestBed.inject(Router).url).toBe('/contact');
        expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toContain('Contact');
    });
});
