using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuizApp.Application.Common.Exceptions;
using QuizApp.Application.DTOs.QuizCodes;
using QuizApp.Application.Interfaces;
using QuizApp.Domain.Entities;

namespace QuizApp.Application.Services;

/// <summary>
/// Manages quiz access codes: self-issue (Start button), validation,
/// and bulk generation for admin sessions.
/// Code format and expiry are driven by configuration, never hardcoded.
/// </summary>
public class QuizCodeService : IQuizCodeService
{
    private readonly IAppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public QuizCodeService(
        IAppDbContext context,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _configuration = configuration;
    }

    // ---- Self-issue (Start button) -------------------------------------

    public async Task<SelfIssueCodeViewModel> SelfIssueAsync(
        Guid quizId,
        Guid userId,
        CancellationToken ct = default)
    {
        if (!await _context.Quizzes.AnyAsync(q => q.Id == quizId && q.IsActive, ct))
            throw new NotFoundException("Quiz not found or is not active.");

        // Return existing unused code if one already exists for (quiz, user).
        var existing = await _context.QuizCodes
            .FirstOrDefaultAsync(qc =>
                qc.QuizId == quizId &&
                qc.UserId == userId &&
                !qc.IsUsed &&
                (qc.ExpiresAt == null || qc.ExpiresAt > DateTimeOffset.UtcNow), ct);

        if (existing != null)
            return new SelfIssueCodeViewModel { QuizCode = existing.Code };

        var code = GenerateCode();
        var expiry = DateTimeOffset.UtcNow.AddHours(ExpiryHours());

        _context.QuizCodes.Add(new QuizCode
        {
            Code = code,
            QuizId = quizId,
            UserId = userId,
            IsUsed = false,
            ExpiresAt = expiry
        });
        await _context.SaveChangesAsync(ct);

        return new SelfIssueCodeViewModel { QuizCode = code };
    }

    // ---- Validation (shared by prepare/take) ---------------------------

    public async Task ValidateAsync(string code, Guid callerUserId, CancellationToken ct = default)
    {
        var quizCode = await _context.QuizCodes
            .FirstOrDefaultAsync(qc => qc.Code == code, ct);

        if (quizCode == null)
            throw new BadRequestException("Quiz code not found.");

        if (quizCode.UserId != callerUserId)
            throw new ForbiddenException("This quiz code belongs to a different user.");

        if (quizCode.IsUsed)
            throw new ConflictException("This quiz code has already been used.",
                new Dictionary<string, string[]> { ["quizCode"] = ["Already used."] });

        if (quizCode.ExpiresAt.HasValue && quizCode.ExpiresAt < DateTimeOffset.UtcNow)
            throw new BadRequestException("This quiz code has expired.");
    }

    // ---- Bulk generation (admin) ---------------------------------------

    public async Task<IReadOnlyList<BulkCodeResultViewModel>> BulkGenerateAsync(
        BulkCodeRequestViewModel request,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(request.QuizId, out var quizId))
            throw new BadRequestException("Invalid quiz id.");

        if (!await _context.Quizzes.AnyAsync(q => q.Id == quizId, ct))
            throw new NotFoundException("Quiz not found.");

        var userIds = request.UserIds
            .Select(uid => Guid.TryParse(uid, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
            throw new BadRequestException("No valid user ids provided.");

        var expiry = DateTimeOffset.UtcNow.AddHours(ExpiryHours());
        var results = new List<BulkCodeResultViewModel>(userIds.Count);

        foreach (var userId in userIds)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) continue;

            var code = GenerateCode();
            _context.QuizCodes.Add(new QuizCode
            {
                Code = code,
                QuizId = quizId,
                UserId = userId,
                IsUsed = false,
                ExpiresAt = expiry
            });

            results.Add(new BulkCodeResultViewModel
            {
                UserId = userId.ToString(),
                UserDisplayName = $"{user.FirstName} {user.LastName}".Trim(),
                Code = code,
                ExpiresAt = expiry.ToString("o")
            });
        }

        await _context.SaveChangesAsync(ct);
        return results;
    }

    // ---- Helpers -------------------------------------------------------

    private string GenerateCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var length = _configuration.GetValue("QuizCode:Length", 8);
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[Random.Shared.Next(chars.Length)])
            .ToArray());
    }

    private double ExpiryHours() => _configuration.GetValue("QuizCode:ExpiryHours", 24.0);
}
