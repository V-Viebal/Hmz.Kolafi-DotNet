namespace Hmz.Kolafi.UnitTests.Core.UserAggregate;

public class UserConstructorTests
{
  [Fact]
  public void CreatesUser_WithValidInputs()
  {
    var user = new User("logto-123", "test@example.com", "Test User");

    user.SubjectId.ShouldBe("logto-123");
    user.Email.ShouldBe("test@example.com");
    user.Name.ShouldBe("Test User");
    user.Picture.ShouldBeNull();
    user.Roles.ShouldBeEmpty();
    user.IsDeleted.ShouldBeFalse();
  }

  [Fact]
  public void CreatesUser_WithRolesAndPicture()
  {
    var roles = new List<string> { "admin", "user" };
    var user = new User("logto-456", "admin@example.com", "Admin User", "https://pic.url", roles);

    user.Roles.ShouldBe(roles);
    user.Picture.ShouldBe("https://pic.url");
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("  ")]
  public void ThrowsException_WhenIdIsInvalid(string? invalidId)
  {
    Should.Throw<ArgumentException>(() => new User(invalidId!, "test@example.com", "Test"));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("  ")]
  public void ThrowsException_WhenEmailIsInvalid(string? invalidEmail)
  {
    Should.Throw<ArgumentException>(() => new User("logto-123", invalidEmail!, "Test"));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("  ")]
  public void ThrowsException_WhenNameIsInvalid(string? invalidName)
  {
    Should.Throw<ArgumentException>(() => new User("logto-123", "test@example.com", invalidName!));
  }
}
