import { Injectable } from '@angular/core';
import { AttemptContent } from '../application/attempt-content';
import { AttemptQuestion, QuizOption } from '../domain/quiz-attempt';

const oopOptions: readonly QuizOption[] = [
  {
    id: 'encapsulation',
    label: 'A',
    title: 'Đóng gói (Encapsulation):',
    description: 'Che giấu dữ liệu nội bộ và chỉ cho phép truy cập qua phương thức.',
  },
  {
    id: 'inheritance',
    label: 'B',
    title: 'Kế thừa (Inheritance):',
    description: 'Cho phép lớp con tái sử dụng và mở rộng các thuộc tính, hành vi của lớp cha.',
  },
  {
    id: 'machine-code',
    label: 'C',
    description: 'Tự động biên dịch sang mã máy mà không cần thông qua trình thông dịch.',
  },
  {
    id: 'polymorphism',
    label: 'D',
    title: 'Đa hình (Polymorphism):',
    description:
      'Cho phép các đối tượng khác nhau phản hồi cùng một thông điệp theo những cách riêng biệt.',
  },
];

const attemptQuestions: readonly AttemptQuestion[] = Array.from(
  { length: 20 },
  (_, index): AttemptQuestion => {
    const number = index + 1;

    if (number === 8) {
      return {
        number,
        prompt: 'Những đặc điểm nào sau đây thuộc về lập trình hướng đối tượng?',
        guidance:
          'Đọc kỹ từng phương án và lựa chọn tất cả các định nghĩa phù hợp với nguyên lý thiết kế phần mềm hướng đối tượng (OOP).',
        options: oopOptions,
        multiple: true,
      };
    }

    return {
      number,
      prompt: `Câu hỏi kiến thức C# số ${number}`,
      guidance: 'Chọn phương án phù hợp nhất trước khi chuyển sang câu hỏi tiếp theo.',
      multiple: false,
      options: [
        { id: 'answer-a', label: 'A', description: 'Phương án A' },
        { id: 'answer-b', label: 'B', description: 'Phương án B' },
        { id: 'answer-c', label: 'C', description: 'Phương án C' },
        { id: 'answer-d', label: 'D', description: 'Phương án D' },
      ],
    };
  },
);

@Injectable()
export class PrototypeAttemptContent implements AttemptContent {
  listQuestions(): readonly AttemptQuestion[] {
    return attemptQuestions;
  }
}
