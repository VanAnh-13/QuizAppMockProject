namespace Quizapp.Application.Abstractions.Messaging;

public sealed class ContactOptions
{
    public const string SectionName = "Contact";
    public string InboxAddress { get; set; } = string.Empty;
}