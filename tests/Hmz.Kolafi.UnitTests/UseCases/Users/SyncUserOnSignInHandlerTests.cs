using Hmz.Kolafi.Core.UserAggregate.Specifications;

namespace Hmz.Kolafi.UnitTests.UseCases.Users;

public class SyncUserOnSignInHandlerTests
{
  private readonly IRepository<User> _repository = Substitute.For<IRepository<User>>();
  private readonly ILogtoUserService _logtoUserService = Substitute.For<ILogtoUserService>();
  private readonly ICachedUserProfileService _cachedProfileService = Substitute.For<ICachedUserProfileService>();
  private readonly ILogger<SyncUserOnSignInHandler> _logger = Substitute.For<ILogger<SyncUserOnSignInHandler>>();
  private readonly SyncUserOnSignInHandler _handler;

  public SyncUserOnSignInHandlerTests()
  {
    _handler = new SyncUserOnSignInHandler(_repository, _logtoUserService, _cachedProfileService, _logger);
  }

  [Fact]
  public async Task ReturnsNotFound_WhenLogtoUserNotFound()
  {
    _logtoUserService.GetUserProfileAsync("unknown-id", Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<LogtoUserProfile?>(null));

    var result = await _handler.Handle(new SyncUserOnSignInCommand("unknown-id"), CancellationToken.None);

    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(Ardalis.Result.ResultStatus.NotFound);
  }

  [Fact]
  public async Task CreatesNewUser_WhenNotInDatabase()
  {
    var logtoProfile = new LogtoUserProfile("user-123", "test@example.com", "Test", null, ["user"]);
    _logtoUserService.GetUserProfileAsync("user-123", Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<LogtoUserProfile?>(logtoProfile));

    // No existing user in DB
    _repository.SingleOrDefaultAsync(Arg.Any<UserBySubjectSpec>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<User?>(null));

    _repository.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
      .Returns(callInfo =>
      {
        var u = callInfo.Arg<User>();
        SetUserId(u, UserId.From(1));
        return Task.FromResult(u);
      });

    var result = await _handler.Handle(new SyncUserOnSignInCommand("user-123"), CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();
    result.Value.SubjectId.ShouldBe("user-123");
    result.Value.Email.ShouldBe("test@example.com");
    result.Value.Roles.ShouldContain("user");

    await _repository.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    await _cachedProfileService.Received(1).InvalidateAsync("user-123", Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task UpdatesExistingUser_WhenAlreadyInDatabase()
  {
    var logtoProfile = new LogtoUserProfile("user-123", "updated@example.com", "Updated Name", "https://new-pic.url", ["admin"]);
    _logtoUserService.GetUserProfileAsync("user-123", Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<LogtoUserProfile?>(logtoProfile));

    var existingUser = new User("user-123", "old@example.com", "Old Name");
    SetUserId(existingUser, UserId.From(1));

    _repository.SingleOrDefaultAsync(Arg.Any<UserBySubjectSpec>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<User?>(existingUser));

    var result = await _handler.Handle(new SyncUserOnSignInCommand("user-123"), CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();
    result.Value.Email.ShouldBe("updated@example.com");
    result.Value.Name.ShouldBe("Updated Name");
    result.Value.Roles.ShouldContain("admin");
    result.Value.LastSignedInAt.ShouldNotBeNull();

    await _repository.Received(1).UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    await _cachedProfileService.Received(1).InvalidateAsync("user-123", Arg.Any<CancellationToken>());
  }

  private static void SetUserId(User user, UserId id)
  {
    var prop = typeof(EntityBase<User, UserId>).GetProperty("Id");
    prop?.SetValue(user, id);
  }
}
