namespace Hmz.Kolafi.Core.UserAggregate.Specifications;

/// <summary>
/// Specification to find a User by their Logto subject claim (SubjectId).
/// </summary>
public class UserBySubjectSpec : SingleResultSpecification<User>
{
  public UserBySubjectSpec(string subjectId)
  {
    Query.Where(u => u.SubjectId == subjectId);
  }
}
