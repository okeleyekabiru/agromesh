namespace Ilevora.Application.DTOs;

/// <summary><see cref="Role"/> is "FarmManager" or "Supervisor".</summary>
public record CreateInvitationRequest(string Email, string Role);

/// <summary>Status is "Pending", "Accepted", "Revoked" or "Expired" (strings, like roles elsewhere in the API).</summary>
public record InvitationDto(string Id, string Email, string Role, string Status, DateTime CreatedAt, DateTime ExpiresAt);

public record InvitationPreviewDto(string Email, string FarmName, string Role);

public record PreviewInvitationRequest(string Token);

public record AcceptInvitationRequest(string Token, string FullName, string Password);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Token, string NewPassword);
