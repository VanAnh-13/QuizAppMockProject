export interface QuizOption {
  readonly id: string;
  readonly label: string;
  readonly title?: string;
  readonly description: string;
}

export interface AttemptQuestion {
  readonly number: number;
  readonly prompt: string;
  readonly guidance: string;
  readonly options: readonly QuizOption[];
  readonly multiple: boolean;
}
