using System;
using System.Collections.Generic;
using System.Linq;
using PAMonitor.XrmToolBox.Models;

namespace PAMonitor.XrmToolBox.Services
{
    /// <summary>
    /// Builds parent/child relationships from flat flowrun rows.
    /// Children may link via parentrunid and/or callingproductrunid.
    /// </summary>
    public static class FlowRunTreeBuilder
    {
        public static IReadOnlyList<FlowRunInfo> GetChildren(FlowRunInfo parent, IReadOnlyList<FlowRunInfo> allRuns)
        {
            if (parent == null || allRuns == null || allRuns.Count == 0)
            {
                return Array.Empty<FlowRunInfo>();
            }

            var parentKeys = GetParentMatchKeys(parent);
            return allRuns
                .Where(r => r != null && !ReferenceEquals(r, parent) && ParentMatches(r, parentKeys))
                .OrderBy(r => r.StartTime ?? DateTime.MaxValue)
                .ThenBy(r => r.FlowName ?? r.RunName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static FlowRunTreeJsonNode ToJsonTree(FlowRunInfo root, IReadOnlyList<FlowRunInfo> allRuns)
        {
            if (root == null)
            {
                return null;
            }

            var visited = new HashSet<Guid>();
            return BuildJsonNode(root, allRuns ?? Array.Empty<FlowRunInfo>(), visited);
        }

        private static FlowRunTreeJsonNode BuildJsonNode(
            FlowRunInfo run,
            IReadOnlyList<FlowRunInfo> allRuns,
            HashSet<Guid> visited)
        {
            if (run == null)
            {
                return null;
            }

            if (run.RunId != Guid.Empty && !visited.Add(run.RunId))
            {
                return CreateJsonNode(run, Array.Empty<FlowRunTreeJsonNode>());
            }

            var children = GetChildren(run, allRuns)
                .Select(child => BuildJsonNode(child, allRuns, visited))
                .Where(n => n != null)
                .ToList();

            return CreateJsonNode(run, children);
        }

        private static FlowRunTreeJsonNode CreateJsonNode(
            FlowRunInfo run,
            IReadOnlyList<FlowRunTreeJsonNode> children)
        {
            return new FlowRunTreeJsonNode
            {
                RunId = run.RunId,
                RunName = run.RunName,
                FlowName = run.FlowName,
                WorkflowId = run.WorkflowId,
                Status = run.Status,
                StartTime = run.StartTime,
                EndTime = run.EndTime,
                DurationMs = run.Duration?.TotalMilliseconds,
                ParentRunName = run.ParentRunName,
                CallingProductRunId = run.CallingProductRunId,
                ClientTrackingId = run.ClientTrackingId,
                ErrorCode = run.ErrorCode,
                ErrorMessage = run.ErrorMessage,
                TriggerType = run.TriggerType,
                IsPrimary = run.IsPrimary,
                ResourceId = run.ResourceId,
                Children = children?.ToList() ?? new List<FlowRunTreeJsonNode>()
            };
        }

        public static bool ParentMatches(FlowRunInfo child, ISet<string> parentKeys)
        {
            if (child == null || parentKeys == null || parentKeys.Count == 0)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(child.ParentRunName)
                && parentKeys.Contains(child.ParentRunName))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(child.CallingProductRunId)
                && parentKeys.Contains(child.CallingProductRunId))
            {
                return true;
            }

            return false;
        }

        public static HashSet<string> GetParentMatchKeys(FlowRunInfo parent)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (parent == null)
            {
                return keys;
            }

            if (!string.IsNullOrWhiteSpace(parent.RunName))
            {
                keys.Add(parent.RunName.Trim());
            }

            if (parent.RunId != Guid.Empty)
            {
                keys.Add(parent.RunId.ToString("D"));
                keys.Add(parent.RunId.ToString("N"));
            }

            return keys;
        }
    }
}
