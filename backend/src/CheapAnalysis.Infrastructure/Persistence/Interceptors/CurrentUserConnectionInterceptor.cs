using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CheapAnalysis.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Scaffold for T-103: sets <c>app.current_user_id</c> on the Postgres session when the
/// connection is opened, and RESETs it on release, so Postgres RLS policies can read it
/// via <c>current_setting('app.current_user_id', true)</c>.
///
/// In T-005 this is a no-op placeholder; T-103 will inject the current ICurrentUser and
/// run the SET/RESET statements.
/// </summary>
public sealed class CurrentUserConnectionInterceptor : DbConnectionInterceptor
{
    public override Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
