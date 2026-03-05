using Hmz.Kolafi.Core.ContributorAggregate;

namespace Hmz.Kolafi.UseCases.Contributors.Get;

public record GetContributorQuery(ContributorId ContributorId) : IQuery<Result<ContributorDto>>;
