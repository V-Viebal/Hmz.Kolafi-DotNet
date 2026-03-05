namespace Hmz.Kolafi.Web.Contributors;

public class UpdateContributorResponse(ContributorUpdatedRecord contributor)
{
  public ContributorUpdatedRecord Contributor { get; set; } = contributor;
}
