using Hmz.Kolafi.Core.ContributorAggregate;
using Hmz.Kolafi.Core.UserAggregate;
using Vogen;

namespace Hmz.Kolafi.Infrastructure.Data.Config;

[EfCoreConverter<ContributorId>]
[EfCoreConverter<ContributorName>]
[EfCoreConverter<UserId>]
internal partial class VogenEfCoreConverters;
