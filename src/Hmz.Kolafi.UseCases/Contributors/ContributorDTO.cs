using Hmz.Kolafi.Core.ContributorAggregate;

namespace Hmz.Kolafi.UseCases.Contributors;

public record ContributorDto(ContributorId Id, ContributorName Name, PhoneNumber PhoneNumber);
