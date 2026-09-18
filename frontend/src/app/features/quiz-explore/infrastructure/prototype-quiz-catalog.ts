import {Injectable} from '@angular/core';
import {QuizCatalog} from '../application/quiz-catalog';
import {QuizSummary} from '../domain/quiz-summary';

const quizzes: readonly QuizSummary[] = [
    {
        id: 'csharp-co-ban-oop',
        categoryId: 'csharp',
        categoryLabel: 'C# / OOP',
        title: 'C# Fundamentals & OOP',
        description: 'Master the four OOP principles, basic C# syntax, value types, and reference types.',
        questionCount: 10,
        durationMinutes: 30,
        status: 'open',
    },
    {
        id: 'sql-server-fundamentals',
        categoryId: 'sql-server',
        categoryLabel: 'SQL Server',
        title: 'SQL Server Fundamentals',
        description: 'Practice JOIN, GROUP BY, index optimization, and database transactions.',
        questionCount: 15,
        durationMinutes: 35,
        status: 'open',
    },
    {
        id: 'angular-routing-forms',
        categoryId: 'angular',
        categoryLabel: 'Angular',
        title: 'Angular Routing & Forms',
        description:
            'Organize components, build forms, protect routes, and handle data with RxJS.',
        questionCount: 12,
        durationMinutes: 25,
        status: 'open',
    },
    {
        id: 'dotnet-core-api-architecture',
        categoryId: 'api',
        categoryLabel: '.NET / Web',
        title: '.NET Application Architecture',
        description:
            'Structure layers, manage dependencies, handle requests, and authorize access in web apps.',
        questionCount: 15,
        durationMinutes: 40,
        status: 'open',
    },
    {
        id: 'typescript-advanced-types',
        categoryId: 'typescript',
        categoryLabel: 'TypeScript',
        title: 'Advanced TypeScript Types',
        description:
            'Put generics, utility types, mapped types, and type narrowing into practice.',
        questionCount: 10,
        durationMinutes: 25,
        status: 'open',
    },
    {
        id: 'csharp-linq-collection-queries',
        categoryId: 'csharp',
        categoryLabel: 'C# / LINQ',
        title: 'LINQ & Collections in C#',
        description:
            'Write efficient LINQ queries and understand deferred execution, IEnumerable, and IQueryable.',
        questionCount: 12,
        durationMinutes: 30,
        status: 'open',
    },
];

import {resolveQuizImageUrl} from '../../../core/utils/quiz-image';

@Injectable()
export class PrototypeQuizCatalog implements QuizCatalog {
    async listQuizzes(): Promise<readonly QuizSummary[]> {
        return quizzes.map((quiz) => ({
            ...quiz,
            imageUrl: resolveQuizImageUrl(quiz.title),
        }));
    }
}
