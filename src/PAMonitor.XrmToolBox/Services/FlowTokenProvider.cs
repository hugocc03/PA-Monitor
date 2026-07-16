using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace PAMonitor.XrmToolBox.Services
{
    /// <summary>
    /// Acquires tokens for api.flow.microsoft.com using a user-registered public client app.
    /// The Dynamics sample client id cannot request Flow tokens (AADSTS65002).
    /// </summary>
    public sealed class FlowTokenProvider
    {
        private static readonly string[] Scopes =
        {
            "https://service.flow.microsoft.com/Flows.Read.All"
        };

        private readonly IPublicClientApplication _app;
        private readonly string _tenantId;
        private AuthenticationResult _cached;

        public FlowTokenProvider(Guid tenantId, string clientId, string redirectUri = null)
        {
            if (tenantId == Guid.Empty)
            {
                throw new ArgumentException("Tenant id is required to call the Flow API.", nameof(tenantId));
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentException(
                    "Flow App Client Id is required. Open Flow API settings and paste your Entra app id.",
                    nameof(clientId));
            }

            if (!Guid.TryParse(clientId.Trim(), out _))
            {
                throw new ArgumentException("Flow App Client Id must be a valid GUID.", nameof(clientId));
            }

            _tenantId = tenantId.ToString("D");
            var redirect = string.IsNullOrWhiteSpace(redirectUri) ? "http://localhost" : redirectUri.Trim();

            _app = PublicClientApplicationBuilder
                .Create(clientId.Trim())
                .WithRedirectUri(redirect)
                .WithTenantId(_tenantId)
                .Build();
        }

        public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            if (_cached != null && _cached.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(2))
            {
                return _cached.AccessToken;
            }

            var accounts = await _app.GetAccountsAsync().ConfigureAwait(false);
            var account = accounts.FirstOrDefault();

            try
            {
                if (account != null)
                {
                    _cached = await _app.AcquireTokenSilent(Scopes, account)
                        .ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    _cached = await AcquireInteractiveAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (MsalUiRequiredException)
            {
                _cached = await AcquireInteractiveAsync(cancellationToken).ConfigureAwait(false);
            }

            return _cached.AccessToken;
        }

        private Task<AuthenticationResult> AcquireInteractiveAsync(CancellationToken cancellationToken)
        {
            return _app.AcquireTokenInteractive(Scopes)
                .WithPrompt(Prompt.SelectAccount)
                .ExecuteAsync(cancellationToken);
        }
    }
}
