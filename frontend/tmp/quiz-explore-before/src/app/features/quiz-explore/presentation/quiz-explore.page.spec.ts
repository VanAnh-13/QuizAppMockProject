import { TestBed } from '@angular/core/testing';
import { QuizExplorePage } from './quiz-explore.page';

describe('QuizExplorePage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [QuizExplorePage],
    }).compileComponents();
  });

  it('renders the Vietnamese explore experience from catalog data', () => {
    const fixture = TestBed.createComponent(QuizExplorePage);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('h1')?.textContent).toContain(
      'Học lập trình với quiz thực chiến',
    );
    expect(element.querySelectorAll('.quiz-card')).toHaveLength(6);
    expect(element.textContent).toContain('6 bài quiz mẫu · 5 chủ đề');
  });

  it('filters the rendered cards when a search is submitted', async () => {
    const fixture = TestBed.createComponent(QuizExplorePage);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    const input = element.querySelector<HTMLInputElement>('#quiz-search');
    const form = element.querySelector<HTMLFormElement>('form[role="search"]');

    expect(input).not.toBeNull();
    expect(form).not.toBeNull();

    input!.value = 'SQL';
    input!.dispatchEvent(new Event('input'));
    form!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(element.querySelectorAll('.quiz-card')).toHaveLength(1);
    expect(element.querySelector('.quiz-card h3')?.textContent).toContain(
      'SQL Server Fundamentals',
    );
  });

  it('filters the rendered cards when a category is selected', () => {
    const fixture = TestBed.createComponent(QuizExplorePage);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    const csharpButton = Array.from(element.querySelectorAll<HTMLButtonElement>('.filters button')).find(
      (button) => button.textContent?.trim() === 'C#/.NET',
    );

    expect(csharpButton).toBeDefined();
    csharpButton!.click();
    fixture.detectChanges();

    expect(element.querySelectorAll('.quiz-card')).toHaveLength(2);
  });
});
