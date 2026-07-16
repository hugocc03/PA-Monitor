using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Organization;

namespace PAMonitor.XrmToolBox.Services
{
    public sealed class EnvironmentContext
    {
        public string EnvironmentId { get; set; }
        public Guid TenantId { get; set; }
        public string Source { get; set; }
    }

    public static class EnvironmentContextResolver
    {
        public static EnvironmentContext Resolve(IOrganizationService service, string connectionEnvironmentId, Guid connectionTenantId)
        {
            if (!string.IsNullOrWhiteSpace(connectionEnvironmentId) && connectionTenantId != Guid.Empty)
            {
                return new EnvironmentContext
                {
                    EnvironmentId = connectionEnvironmentId.Trim(),
                    TenantId = connectionTenantId,
                    Source = "connection"
                };
            }

            if (service == null)
            {
                return null;
            }

            var fromOrg = ResolveFromOrganization(service);
            if (fromOrg == null)
            {
                return null;
            }

            // Fill gaps from connection if partially available
            if (string.IsNullOrWhiteSpace(fromOrg.EnvironmentId) && !string.IsNullOrWhiteSpace(connectionEnvironmentId))
            {
                fromOrg.EnvironmentId = connectionEnvironmentId.Trim();
            }

            if (fromOrg.TenantId == Guid.Empty && connectionTenantId != Guid.Empty)
            {
                fromOrg.TenantId = connectionTenantId;
            }

            return string.IsNullOrWhiteSpace(fromOrg.EnvironmentId) || fromOrg.TenantId == Guid.Empty
                ? null
                : fromOrg;
        }

        private static EnvironmentContext ResolveFromOrganization(IOrganizationService service)
        {
            try
            {
                var response = (RetrieveCurrentOrganizationResponse)service.Execute(
                    new RetrieveCurrentOrganizationRequest
                    {
                        AccessType = EndpointAccessType.Default
                    });

                var detail = response?.Detail;
                if (detail == null)
                {
                    return null;
                }

                var tenantId = Guid.Empty;
                if (!string.IsNullOrWhiteSpace(detail.TenantId))
                {
                    Guid.TryParse(detail.TenantId, out tenantId);
                }

                return new EnvironmentContext
                {
                    EnvironmentId = detail.EnvironmentId,
                    TenantId = tenantId,
                    Source = "RetrieveCurrentOrganization"
                };
            }
            catch
            {
                // Fallback for older orgs / unexpected response shape
                return ResolveFromOrganizationRequest(service);
            }
        }

        private static EnvironmentContext ResolveFromOrganizationRequest(IOrganizationService service)
        {
            try
            {
                var request = new OrganizationRequest("RetrieveCurrentOrganization");
                request["AccessType"] = EndpointAccessType.Default;
                var response = service.Execute(request);
                if (response == null || !response.Results.Contains("Detail"))
                {
                    return null;
                }

                var detail = response["Detail"] as OrganizationDetail;
                if (detail == null)
                {
                    return null;
                }

                var tenantId = Guid.Empty;
                if (!string.IsNullOrWhiteSpace(detail.TenantId))
                {
                    Guid.TryParse(detail.TenantId, out tenantId);
                }

                return new EnvironmentContext
                {
                    EnvironmentId = detail.EnvironmentId,
                    TenantId = tenantId,
                    Source = "RetrieveCurrentOrganization (late)"
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
