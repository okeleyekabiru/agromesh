namespace AgroMesh.Domain.Enums;

/// <summary>
/// Minimal role gate — just enough to distinguish "can approve a stock adjustment" from "can't".
/// Not a general permissions system: no invite flow, no multi-role hierarchy beyond these two.
/// </summary>
public enum UserRole { Admin, Member }
