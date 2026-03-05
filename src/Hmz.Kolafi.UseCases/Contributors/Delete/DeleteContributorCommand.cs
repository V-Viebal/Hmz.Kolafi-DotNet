using Hmz.Kolafi.Core.ContributorAggregate;

namespace Hmz.Kolafi.UseCases.Contributors.Delete;

public record DeleteContributorCommand(ContributorId ContributorId) : ICommand<Result>;
