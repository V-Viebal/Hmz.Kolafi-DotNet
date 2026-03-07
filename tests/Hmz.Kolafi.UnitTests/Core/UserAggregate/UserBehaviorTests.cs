using Hmz.Kolafi.Core.UserAggregate.Events;

namespace Hmz.Kolafi.UnitTests.Core.UserAggregate;

public class UserBehaviorTests
{
  private User CreateTestUser() =>
    new("logto-123", "test@example.com", "Test User", null, ["user"]);

  [Fact]
  public void UpdateProfile_UpdatesAllFields()
  {
    var user = CreateTestUser();

    user.UpdateProfile("new@example.com", "New Name", "https://pic.url", ["admin", "user"]);

    user.Email.ShouldBe("new@example.com");
    user.Name.ShouldBe("New Name");
    user.Picture.ShouldBe("https://pic.url");
    user.Roles.ShouldBe(["admin", "user"]);
  }

  [Fact]
  public void UpdateProfile_PreservesRoles_WhenNull()
  {
    var user = CreateTestUser();
    var originalRoles = user.Roles.ToList();

    user.UpdateProfile("new@example.com", "New Name", null, roles: null);

    user.Roles.ShouldBe(originalRoles);
  }

  [Fact]
  public void UpdateProfile_ThrowsOnInvalidEmail()
  {
    var user = CreateTestUser();
    Should.Throw<ArgumentException>(() => user.UpdateProfile("", "Name", null, null));
  }

  [Fact]
  public void UpdateProfile_ThrowsOnInvalidName()
  {
    var user = CreateTestUser();
    Should.Throw<ArgumentException>(() => user.UpdateProfile("test@example.com", "", null, null));
  }

  [Fact]
  public void RecordSignIn_SetsLastSignedInAt()
  {
    var user = CreateTestUser();
    user.LastSignedInAt.ShouldBeNull();

    user.RecordSignIn();

    user.LastSignedInAt.ShouldNotBeNull();
  }

  [Fact]
  public void RecordSignIn_RaisesUserSignedInEvent()
  {
    var user = CreateTestUser();

    user.RecordSignIn();

    user.DomainEvents.ShouldContain(e => e is UserSignedInEvent);
    var @event = (UserSignedInEvent)user.DomainEvents.First(e => e is UserSignedInEvent);
    @event.User.ShouldBe(user);
  }

  [Fact]
  public void SoftDelete_SetsDeletedFields()
  {
    var user = CreateTestUser();
    var now = DateTimeOffset.UtcNow;

    user.SoftDelete(now, "admin-user");

    user.IsDeleted.ShouldBeTrue();
    user.DeletedAt.ShouldBe(now);
    user.DeletedBy.ShouldBe("admin-user");
  }

  [Fact]
  public void Restore_ClearsDeletedFields()
  {
    var user = CreateTestUser();
    user.SoftDelete(DateTimeOffset.UtcNow, "admin");

    user.Restore();

    user.IsDeleted.ShouldBeFalse();
    user.DeletedAt.ShouldBeNull();
    user.DeletedBy.ShouldBeNull();
  }

  [Fact]
  public void SetCreated_SetsAuditFields()
  {
    var user = CreateTestUser();
    var now = DateTimeOffset.UtcNow;

    user.SetCreated(now, "system");

    user.CreatedAt.ShouldBe(now);
    user.CreatedBy.ShouldBe("system");
  }

  [Fact]
  public void SetModified_SetsAuditFields()
  {
    var user = CreateTestUser();
    var now = DateTimeOffset.UtcNow;

    user.SetModified(now, "edit-user");

    user.ModifiedAt.ShouldBe(now);
    user.ModifiedBy.ShouldBe("edit-user");
  }
}
