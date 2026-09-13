import { Injectable } from '@angular/core';
import { QuizDetailsContent, QuizDetailsSnapshot } from '../application/quiz-details-content';

const quizDetailsSnapshot: QuizDetailsSnapshot = {
  title: 'C# Cơ bản & Lập trình hướng đối tượng (OOP)',
  description:
    'Bài kiểm tra đánh giá toàn diện năng lực lập trình hướng đối tượng trong hệ sinh thái C# & .NET. Thử thách tập trung vào các nguyên lý cốt lõi OOP, quản lý bộ nhớ CLR, các cấu trúc cú pháp hiện đại và ứng dụng thực tiễn trong xây dựng ứng dụng doanh nghiệp.',
  categoryLabel: 'C# / .NET',
  metrics: [
    { icon: 'quiz', label: 'Số câu hỏi', value: '10 câu hỏi' },
    { icon: 'timer', label: 'Thời lượng', value: '30 phút' },
    { icon: 'signal_cellular_alt', label: 'Độ khó', value: 'Trung bình' },
    { icon: 'military_tech', label: 'Điểm đạt', value: '70%' },
  ],
  topics: [
    {
      title: 'Lớp & Đối tượng',
      description: 'Class, Object, Constructor và các mức độ truy cập Access Modifiers chuẩn .NET.',
    },
    {
      title: '4 Tính chất cốt lõi OOP',
      description:
        'Kế thừa (Inheritance), Đa hình (Polymorphism), Đóng gói & Trừu tượng hóa (Interface / Abstract).',
    },
    {
      title: 'Quản lý ngoại lệ & GC',
      description:
        'Exception Handling (try-catch-finally), Custom Exceptions và cơ chế dọn rác GC CLR.',
    },
    {
      title: 'Cú pháp hiện đại & LINQ',
      description:
        'Pattern matching, record, nullable reference types và truy vấn dữ liệu LINQ cơ bản.',
    },
  ],
  guidelines: [
    {
      title: 'Thời gian làm bài:',
      description: 'Đồng hồ đếm ngược 30 phút sẽ kích hoạt ngay khi bạn nhấn "Bắt đầu làm bài".',
    },
    {
      title: 'Chuyển đổi câu hỏi tự do:',
      description:
        'Bạn có thể nhảy cóc giữa 10 câu hỏi, gắn cờ xem lại và đổi câu trả lời bất kỳ lúc nào trước khi nộp.',
    },
    {
      title: 'Cơ chế tự động thu bài:',
      description:
        'Khi đồng hồ về 00:00, bài thi sẽ tự động khóa và gửi đáp án về máy chủ để chấm điểm.',
    },
    {
      title: 'Đánh giá tức thì:',
      description:
        'Điểm số, chứng chỉ thành tích và bảng giải thích chi tiết cho từng câu sẽ xuất hiện ngay sau khi kết thúc.',
    },
  ],
  formatFacts: [
    { label: 'Dạng câu hỏi', value: 'Trắc nghiệm (1 hoặc nhiều đáp án)' },
    { label: 'Đoạn mã code mẫu', value: '6 câu hỏi có code block' },
    { label: 'Số lần thi lại', value: 'Không giới hạn' },
  ],
};

@Injectable()
export class PrototypeQuizDetailsContent implements QuizDetailsContent {
  async load(_quizSlug = 'csharp-co-ban-oop'): Promise<QuizDetailsSnapshot> {
    return quizDetailsSnapshot;
  }
}
