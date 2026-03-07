namespace Hmz.Kolafi.UseCases.Users.GetProfile;

public record GetMyProfileQuery(string UserId) : IQuery<Result<UserProfileDTO>>;
