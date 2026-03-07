namespace Hmz.Kolafi.Core.UserAggregate;

public record UserProfileDTO(
  int Id,
  string SubjectId,
  string Email,
  string Name,
  string? Picture,
  List<string> Roles,
  DateTimeOffset? LastSignedInAt);
