using System;
using PAMonitor.XrmToolBox.Models;

namespace PAMonitor.XrmToolBox.Services
{
    public static class FlowRunUrlBuilder
    {
        /// <summary>
        /// Portal Maker URL for a cloud flow run.
        /// Uses Dataverse workflowid (xrmWorkflowId), not flowrun.resourceid (runtime / -azshadow).
        /// https://make.powerautomate.com/environments/{env}/flows/{workflowId}/runs/{runName}
        /// </summary>
        public static string Build(string environmentId, FlowRunInfo run)
        {
            if (string.IsNullOrWhiteSpace(environmentId) || run == null)
            {
                return null;
            }

            if (run.WorkflowId == Guid.Empty || string.IsNullOrWhiteSpace(run.RunName))
            {
                return null;
            }

            var flowId = run.WorkflowId.ToString("D");
            var runName = run.RunName.Trim();

            return
                "https://make.powerautomate.com/environments/" +
                Uri.EscapeDataString(environmentId.Trim()) +
                "/flows/" +
                Uri.EscapeDataString(flowId) +
                "/runs/" +
                Uri.EscapeDataString(runName);
        }
    }
}
