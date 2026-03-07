using System.Security.Claims;
using Hmz.Kolafi.Core.FeatureFlags;
using Hmz.Kolafi.UseCases.Users;
using Hmz.Kolafi.UseCases.Users.GetProfile;
using Hmz.Kolafi.Web.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Hmz.Kolafi.Web.Users;

/// <summary>
/// GET /api/v1/users/profiles/me — returns the current authenticated user's profile.
/// </summary>
[ModuleFeature(FeatureFlags.UsersModule)]
public class GetMyProfile(IMediator mediator)
  : EndpointWithoutRequest<
      Results<Ok<UserProfileResponse>,
              NotFound,
              ProblemHttpResult>>
{
  public override void Configure()
  {
    Get("/api/v1/users/profiles/me");
    // Requires authentication
    Summary(s =>
    {
      s.Summary = "Get my profile";
      s.Description = "Returns the authenticated user's profile from cache/database. Requires a valid JWT token.";
      s.Responses[200] = "Profile returned successfully";
      s.Responses[401] = "Not authenticated";
      s.Responses[404] = "User profile not found — sign in first";
    });
    Tags("Users");
    Description(builder => builder
      .Produces<UserProfileResponse>(200, "application/json")
      .ProducesProblem(401)
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<UserProfileResponse>, NotFound, ProblemHttpResult>>
    ExecuteAsync(CancellationToken ct)
  {
    var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
              ?? HttpContext.User.FindFirstValue("sub");

    if (string.IsNullOrEmpty(userId))
    {
      return TypedResults.Problem(
        title: "Unauthorized",
        detail: "User identity not found in token.",
        statusCode: StatusCodes.Status401Unauthorized);
    }

    var result = await mediator.Send(new GetMyProfileQuery(userId), ct);

    return result.ToGetByIdResult(dto => new UserProfileResponse(
      dto.Id,
      dto.SubjectId,
      dto.Email,
      dto.Name,
      dto.Picture,
      dto.Roles,
      dto.LastSignedInAt));
  }
}

public record UserProfileResponse(
  int Id,
  string SubjectId,
  string Email,
  string Name,
  string? Picture,
  List<string> Roles,
  DateTimeOffset? LastSignedInAt);
