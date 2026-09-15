import {ViewportScroller} from '@angular/common';
import {ApplicationRef, APP_BOOTSTRAP_LISTENER} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {provideRouter, Router} from '@angular/router';
import {App} from './app';
import {appConfig} from './app.config';
import {PrototypeQuizCatalog} from './features/quiz-explore/infrastructure/prototype-quiz-catalog';
import {QUIZ_CATALOG} from './features/quiz-explore/infrastructure/quiz-catalog.provider';
import {QuizExplorePage} from './features/quiz-explore/presentation/quiz-explore.page';
import {QuizExploreStore} from './features/quiz-explore/presentation/quiz-explore.store';

describe('App', () => {
    it('creates the routed application shell', () => {
        TestBed.configureTestingModule({imports: [App], providers: [provideRouter([])]});

        expect(TestBed.createComponent(App).componentInstance).toBeTruthy();
    });

    it('scrolls to the catalog anchor when navigating to the quiz-list fragment', async () => {
        await TestBed.configureTestingModule({imports: [App], providers: [...appConfig.providers]})
            .overrideComponent(QuizExplorePage, {
                set: {providers: [{provide: QUIZ_CATALOG, useClass: PrototypeQuizCatalog}, QuizExploreStore]},
            })
            .compileComponents();
        const scroller = TestBed.inject(ViewportScroller);
        const scrollToAnchor = vi.spyOn(scroller, 'scrollToAnchor').mockImplementation(() => {});
        vi.spyOn(scroller, 'scrollToPosition').mockImplementation(() => {});
        const fixture = TestBed.createComponent(App);
        fixture.detectChanges();
        const appRef = TestBed.inject(ApplicationRef);

        // RouterScroller.init() only runs through bootstrap listeners, which TestBed never triggers.
        for (const listener of TestBed.inject(APP_BOOTSTRAP_LISTENER)) {
            listener(appRef.components[0]);
        }

        await appRef.whenStable();
        await TestBed.inject(Router).navigateByUrl('/#quiz-list');
        await appRef.whenStable();
        // The router schedules the scroll on a macrotask outside the zone; flush real timers.
        await new Promise((resolve) => setTimeout(resolve, 50));

        expect(scrollToAnchor).toHaveBeenCalledWith('quiz-list');
    });
});
