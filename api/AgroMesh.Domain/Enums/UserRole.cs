namespace AgroMesh.Domain.Enums;

/// <summary>
/// Admin: the company owner (created by registration). FarmManager: added by an Admin only.
/// Supervisor: added by an Admin or a FarmManager. Member is the legacy/fail-closed default for a
/// token whose role can't be determined — it can't be assigned to a new user.
/// </summary>
public enum UserRole { Admin, Member, FarmManager, Supervisor }
