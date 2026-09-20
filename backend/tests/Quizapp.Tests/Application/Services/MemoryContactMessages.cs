using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Domain.Entities;

namespace Quizapp.Tests.Application.Services;

internal sealed class MemoryContactMessages : IContactMessageRepository
{
    public List<ContactMessage> Rows { get; } = [];

    public void Add(ContactMessage message) => Rows.Add(message);
}
