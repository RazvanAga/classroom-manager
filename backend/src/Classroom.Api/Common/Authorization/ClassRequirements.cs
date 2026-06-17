using Microsoft.AspNetCore.Authorization;

namespace Classroom.Api.Common.Authorization;

/// <summary>The current teacher must be a member of the target class (any role).</summary>
public class ClassMembershipRequirement : IAuthorizationRequirement;

/// <summary>The current teacher must be an Owner of the target class (gates destructive ops).</summary>
public class ClassOwnerRequirement : IAuthorizationRequirement;
