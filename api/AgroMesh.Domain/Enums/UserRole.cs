namespace AgroMesh.Domain.Enums;

/// <summary>
/// Admin: the company owner (created by registration). An Admin invites FarmManagers and Supervisors and is the only one
/// who can disable accounts. FarmManager: does everything else and may invite Supervisors, but not other managers. Member is the legacy/fail-closed default for a
/// token whose role can't be determined — it can't be assigned to a new user.
/// </summary>
public enum UserRole { Admin, Member, FarmManager, Supervisor }
