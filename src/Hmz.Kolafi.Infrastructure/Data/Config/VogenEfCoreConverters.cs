using Hmz.Kolafi.Core.ContributorAggregate;
using Vogen;

namespace Hmz.Kolafi.Infrastructure.Data.Config;

[EfCoreConverter<ContributorId>]
[EfCoreConverter<ContributorName>]
internal partial class VogenEfCoreConverters;
