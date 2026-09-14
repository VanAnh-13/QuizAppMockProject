import {AttemptQuestion} from '../domain/quiz-attempt';

export interface AttemptContent {
    listQuestions(): readonly AttemptQuestion[];
}
