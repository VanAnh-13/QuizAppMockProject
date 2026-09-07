namespace Quizapp.Api.Controllers;

/// <summary>Shared request body for activate/deactivate endpoints.</summary>
public sealed record SetActiveRequest(bool IsActive);
