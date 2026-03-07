using Hmz.Kolafi.Core.Interfaces;

namespace Hmz.Kolafi.UseCases.Users.GetProfile;

public class GetMyProfileHandler(ICachedUserProfileService _cachedProfileService)
  : IQueryHandler<GetMyProfileQuery, Result<UserProfileDTO>>
{
  public async ValueTask<Result<UserProfileDTO>> Handle(GetMyProfileQuery query, CancellationToken ct)
  {
    var profile = await _cachedProfileService.GetProfileAsync(query.UserId, ct);
    if (profile is null)
    {
      return Result.NotFound("User profile not found.");
    }

    return profile;
  }
}
