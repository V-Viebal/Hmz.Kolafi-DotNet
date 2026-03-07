using Vogen;

namespace Hmz.Kolafi.Core.UserAggregate;

/// <summary>
/// Strongly typed ID for User.
/// </summary>
[ValueObject<int>]
public readonly partial struct UserId { }
