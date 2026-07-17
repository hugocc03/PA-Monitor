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
