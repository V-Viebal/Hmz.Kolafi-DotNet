using Hmz.Kolafi.Core.Interfaces;
using Hmz.Kolafi.Core.UserAggregate;
using Hmz.Kolafi.Core.UserAggregate.Specifications;
using Microsoft.Extensions.Logging;

namespace Hmz.Kolafi.UseCases.Users.SignIn;

public class SyncUserOnSignInHandler(
  IRepository<User> _repository,
  ILogtoUserService _logtoUserService,
  ICachedUserProfileService _cachedProfileService,
  ILogger<SyncUserOnSignInHandler> _logger)
  : ICommandHandler<SyncUserOnSignInCommand, Result<UserProfileDTO>>
{
  public async ValueTask<Result<UserProfileDTO>> Handle(SyncUserOnSignInCommand command, CancellationToken ct)
  {
    _logger.LogInformation("Syncing user profile on sign-in for {UserId}", command.UserId);

    // 1. Fetch user profile from Logto Management API
    var logtoProfile = await _logtoUserService.GetUserProfileAsync(command.UserId, ct);
    if (logtoProfile is null)
    {
      _logger.LogWarning("User {UserId} not found in Logto", command.UserId);
      return Result.NotFound("User not found in identity provider.");
    }

    // 2. Upsert user in our database
    var spec = new UserBySubjectSpec(command.UserId);
    var existingUser = await _repository.SingleOrDefaultAsync(spec, ct);

    User user;
    if (existingUser is not null)
    {
      user = existingUser
        .UpdateProfile(logtoProfile.Email, logtoProfile.Name, logtoProfile.Picture, logtoProfile.Roles)
        .RecordSignIn();
      await _repository.UpdateAsync(user, ct);
      _logger.LogInformation("Updated existing user {UserId}", command.UserId);
    }
    else
    {
      user = new User(logtoProfile.Id, logtoProfile.Email, logtoProfile.Name, logtoProfile.Picture, logtoProfile.Roles);
      user.RecordSignIn();
      await _repository.AddAsync(user, ct);
      _logger.LogInformation("Created new user {UserId}", command.UserId);
    }

    // 3. Invalidate cache so next read picks up fresh data
    await _cachedProfileService.InvalidateAsync(command.UserId, ct);

    var dto = new UserProfileDTO(
        user.Id.Value,
        user.SubjectId,
        user.Email,
        user.Name,
        user.Picture,
        user.Roles,
        user.LastSignedInAt);
    return Result.Success(dto);
  }
}
