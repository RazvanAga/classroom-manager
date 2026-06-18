using Microsoft.AspNetCore.Authorization;

namespace Classroom.Api.Common.Authorization;

/// <summary>The current teacher must be a member of the target class (any role).</summary>
public class ClassMembershipRequirement : IAuthorizationRequirement;

/// <summary>The current teacher must be an Owner of the target class (gates destructive ops).</summary>
public class ClassOwnerRequirement : IAuthorizationRequirement;

/// <summary>
/// Satisfied by either a member-teacher of the target class <b>or</b> a kiosk principal scoped to
/// that same class. Gates the only endpoints a kiosk session may reach — roster read, shop, equip
/// (design.md §5.4).
/// </summary>
public class KioskOrMemberRequirement : IAuthorizationRequirement;
