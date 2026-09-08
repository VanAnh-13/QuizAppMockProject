using System.ComponentModel.DataAnnotations;

namespace Quizapp.Api.Controllers;

/// <summary>Shared request body for activate/deactivate endpoints.</summary>
public sealed record SetActiveRequest
{
    /// <summary>Required. Pass <c>true</c> to activate or <c>false</c> to deactivate.</summary>
    [Required]
    public bool? IsActive { get; init; }
}
