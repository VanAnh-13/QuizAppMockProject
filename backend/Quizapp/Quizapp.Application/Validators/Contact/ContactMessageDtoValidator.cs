using FluentValidation;
using Quizapp.Application.DTOs.Contact;
using Quizapp.Domain.Constants;

namespace Quizapp.Application.Validators.Contact;

public sealed class ContactMessageDtoValidator : AbstractValidator<ContactMessageDto>
{
    public ContactMessageDtoValidator()
    {
        RuleFor(dto => dto.FullName)
            .NotEmpty()
            .MaximumLength(FieldLimits.FullNameLength);

        RuleFor(dto => dto.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(FieldLimits.EmailLength);

        RuleFor(dto => dto.Subject)
            .MaximumLength(FieldLimits.QuizTitleLength);

        RuleFor(dto => dto.Message)
            .NotEmpty()
            .MaximumLength(FieldLimits.ContentLength);
    }
}
