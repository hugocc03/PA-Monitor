using System;
using PAMonitor.XrmToolBox.Models;

namespace PAMonitor.XrmToolBox.Services
{
    public static class FlowRunUrlBuilder
    {
        /// <summary>
        /// https://make.powerautomate.com/environments/{env}/flows/{flowId}/runs/{runName}
        /// </summary>
        public static string Build(string environmentId, FlowRunInfo run)
        {
            if (string.IsNullOrWhiteSpace(environmentId) || run == null)
            {
                return null;
            }

            var flowId = !string.IsNullOrWhiteSpace(run.ResourceId)
                ? run.ResourceId.Trim()
                : run.WorkflowId == Guid.Empty ? null : run.WorkflowId.ToString("D");

            var runName = run.RunName;
            if (string.IsNullOrWhiteSpace(flowId) || string.IsNullOrWhiteSpace(runName))
            {
                return null;
            }

            return
                "https://make.powerautomate.com/environments/" +
                Uri.EscapeDataString(environmentId.Trim()) +
                "/flows/" +
                Uri.EscapeDataString(flowId) +
                "/runs/" +
                Uri.EscapeDataString(runName.Trim());
        }
    }
}
