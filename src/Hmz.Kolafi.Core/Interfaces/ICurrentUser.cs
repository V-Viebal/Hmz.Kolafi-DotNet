namespace Hmz.Kolafi.Core.Interfaces;

/// <summary>
/// Provides the identity of the currently authenticated user.
/// Implemented in the Web layer using HttpContext claims.
/// Infrastructure interceptors use this to stamp audit fields.
/// </summary>
public interface ICurrentUser
{
  /// <summary>
  /// The Logto subject ID (JWT 'sub' claim) of the current user.
  /// Returns null for anonymous/system operations.
  /// </summary>
  string? UserId { get; }
}
