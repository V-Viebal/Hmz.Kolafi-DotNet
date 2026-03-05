using Hmz.Kolafi.Core.ContributorAggregate;

namespace Hmz.Kolafi.UseCases.Contributors.Update;

public record UpdateContributorCommand(ContributorId ContributorId, ContributorName NewName) : ICommand<Result<ContributorDto>>;
