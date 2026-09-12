import { TestBed } from '@angular/core/testing';
import { quizCatalogProvider } from '../data/quiz-catalog.provider';
import { QuizExploreStore } from './quiz-explore.store';

describe('QuizExploreStore', () => {
  let store: QuizExploreStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [quizCatalogProvider, QuizExploreStore],
    });
    store = TestBed.inject(QuizExploreStore);
  });

  it('loads catalog content through the catalog port', () => {
    expect(store.totalCatalogCount).toBe(6);
    expect(store.categoryCount).toBe(5);
    expect(store.visibleQuizzes()).toHaveLength(6);
  });

  it('filters quizzes by category and resets the current page', () => {
    store.goToPage(2);

    store.selectCategory('csharp');

    expect(store.currentPage()).toBe(1);
    expect(store.visibleQuizzes().map((quiz) => quiz.title)).toEqual([
      'C# Cơ bản & OOP',
      'C# LINQ & Collection Queries',
    ]);
  });

  it('searches across quiz titles, descriptions, and category labels', () => {
    store.applySearch('SQL');

    expect(store.visibleQuizzes()).toHaveLength(1);
    expect(store.visibleQuizzes()[0]?.title).toBe('SQL Server Fundamentals');
  });
});
