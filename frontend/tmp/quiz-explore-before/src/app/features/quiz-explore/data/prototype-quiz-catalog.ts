import { Injectable } from '@angular/core';
import { QuizCatalog } from '../application/quiz-catalog';
import { QuizCategory, QuizSummary } from '../domain/quiz-summary';

const categories: readonly QuizCategory[] = [
  { id: 'csharp', label: 'C#/.NET' },
  { id: 'sql-server', label: 'SQL Server' },
  { id: 'angular', label: 'Angular' },
  { id: 'typescript', label: 'TypeScript' },
  { id: 'api', label: 'API' },
];

const quizzes: readonly QuizSummary[] = [
  {
    id: 'csharp-co-ban-oop',
    categoryId: 'csharp',
    categoryLabel: 'C# / OOP',
    title: 'C# Cơ bản & OOP',
    description:
      'Nắm vững bốn tính chất OOP, cú pháp C# căn bản, kiểu giá trị và kiểu tham chiếu.',
    questionCount: 10,
    durationMinutes: 30,
    status: 'open',
  },
  {
    id: 'sql-server-fundamentals',
    categoryId: 'sql-server',
    categoryLabel: 'SQL Server',
    title: 'SQL Server Fundamentals',
    description: 'Truy vấn JOIN, GROUP BY, tối ưu index và transaction an toàn trong RDBMS.',
    questionCount: 15,
    durationMinutes: 35,
    status: 'open',
  },
  {
    id: 'angular-routing-forms',
    categoryId: 'angular',
    categoryLabel: 'Angular',
    title: 'Angular Routing & Forms',
    description: 'Kiến trúc component, Reactive Forms, guards và luồng xử lý dữ liệu RxJS.',
    questionCount: 12,
    durationMinutes: 25,
    status: 'open',
  },
  {
    id: 'dotnet-core-api-architecture',
    categoryId: 'api',
    categoryLabel: 'Web API',
    title: '.NET Core API Architecture',
    description:
      'Thiết kế RESTful API, Dependency Injection, middleware pipeline và phân quyền ứng dụng.',
    questionCount: 15,
    durationMinutes: 40,
    status: 'open',
  },
  {
    id: 'typescript-advanced-types',
    categoryId: 'typescript',
    categoryLabel: 'TypeScript',
    title: 'TypeScript Advanced Types',
    description: 'Generics, Utility Types, Mapped Types và Type Narrowing ứng dụng thực tế.',
    questionCount: 10,
    durationMinutes: 25,
    status: 'open',
  },
  {
    id: 'csharp-linq-collection-queries',
    categoryId: 'csharp',
    categoryLabel: 'C# / LINQ',
    title: 'C# LINQ & Collection Queries',
    description:
      'Tối ưu biểu thức truy vấn LINQ, Deferred Execution, IEnumerable và IQueryable.',
    questionCount: 12,
    durationMinutes: 30,
    status: 'open',
  },
];

@Injectable()
export class PrototypeQuizCatalog implements QuizCatalog {
  listCategories(): readonly QuizCategory[] {
    return categories;
  }

  listQuizzes(): readonly QuizSummary[] {
    return quizzes;
  }
}
