namespace Hmz.Kolafi.UnitTests.UseCases.Users;

public class GetMyProfileHandlerTests
{
  private readonly ICachedUserProfileService _cachedProfileService = Substitute.For<ICachedUserProfileService>();
  private readonly GetMyProfileHandler _handler;

  public GetMyProfileHandlerTests()
  {
    _handler = new GetMyProfileHandler(_cachedProfileService);
  }

  [Fact]
  public async Task ReturnsProfile_WhenCacheHit()
  {
    var cachedProfile = new UserProfileDTO(1, "user-123", "test@example.com", "Test User", null, ["user"], null);
    _cachedProfileService.GetProfileAsync("user-123", Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<UserProfileDTO?>(cachedProfile));

    var result = await _handler.Handle(new GetMyProfileQuery("user-123"), CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();
    result.Value.Id.ShouldBe(1);
    result.Value.SubjectId.ShouldBe("user-123");
    result.Value.Email.ShouldBe("test@example.com");
    result.Value.Roles.ShouldContain("user");
  }

  [Fact]
  public async Task ReturnsNotFound_WhenUserDoesNotExist()
  {
    _cachedProfileService.GetProfileAsync("missing-user", Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<UserProfileDTO?>(null));

    var result = await _handler.Handle(new GetMyProfileQuery("missing-user"), CancellationToken.None);

    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(Ardalis.Result.ResultStatus.NotFound);
  }
}
