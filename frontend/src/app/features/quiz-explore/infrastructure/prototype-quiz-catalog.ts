import {Injectable} from '@angular/core';
import {QuizCatalog} from '../application/quiz-catalog';
import {QuizSummary} from '../domain/quiz-summary';

const quizzes: readonly QuizSummary[] = [
    {
        id: 'csharp-co-ban-oop',
        categoryId: 'csharp',
        categoryLabel: 'C# / OOP',
        title: 'C# Cơ bản & OOP',
        description: 'Nắm vững bốn tính chất OOP, cú pháp C# căn bản, kiểu giá trị và kiểu tham chiếu.',
        questionCount: 10,
        durationMinutes: 30,
        status: 'open',
    },
    {
        id: 'sql-server-fundamentals',
        categoryId: 'sql-server',
        categoryLabel: 'SQL Server',
        title: 'Nền tảng SQL Server',
        description: 'Thực hành JOIN, GROUP BY, tối ưu chỉ mục và xử lý giao dịch trong cơ sở dữ liệu.',
        questionCount: 15,
        durationMinutes: 35,
        status: 'open',
    },
    {
        id: 'angular-routing-forms',
        categoryId: 'angular',
        categoryLabel: 'Angular',
        title: 'Điều hướng & biểu mẫu Angular',
        description:
            'Tổ chức thành phần, xây dựng biểu mẫu, bảo vệ điều hướng và xử lý dữ liệu với RxJS.',
        questionCount: 12,
        durationMinutes: 25,
        status: 'open',
    },
    {
        id: 'dotnet-core-api-architecture',
        categoryId: 'api',
        categoryLabel: '.NET / Web',
        title: 'Kiến trúc ứng dụng .NET',
        description:
            'Tổ chức các lớp, quản lý phụ thuộc, xử lý yêu cầu và phân quyền trong ứng dụng web.',
        questionCount: 15,
        durationMinutes: 40,
        status: 'open',
    },
    {
        id: 'typescript-advanced-types',
        categoryId: 'typescript',
        categoryLabel: 'TypeScript',
        title: 'Kiểu dữ liệu nâng cao TypeScript',
        description:
            'Ứng dụng kiểu tổng quát, kiểu tiện ích, kiểu ánh xạ và thu hẹp kiểu trong thực tế.',
        questionCount: 10,
        durationMinutes: 25,
        status: 'open',
    },
    {
        id: 'csharp-linq-collection-queries',
        categoryId: 'csharp',
        categoryLabel: 'C# / LINQ',
        title: 'LINQ & truy vấn tập hợp trong C#',
        description:
            'Viết truy vấn LINQ hiệu quả, hiểu cơ chế thực thi trì hoãn, IEnumerable và IQueryable.',
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
