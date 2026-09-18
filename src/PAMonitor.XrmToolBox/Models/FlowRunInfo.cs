using System;
using System.Collections.Generic;

namespace PAMonitor.XrmToolBox.Models
{
    public enum FlowRunStatus
    {
        All = 0,
        Succeeded = 1,
        Failed = 2,
        Cancelled = 3,
        Running = 4,
        Waiting = 5
    }

    public sealed class FlowRunInfo
    {
        public Guid RunId { get; set; }
        /// <summary>Logic Apps run name (flowrun.name). Child rows match parentrunid to this value.</summary>
        public string RunName { get; set; }
        public Guid WorkflowId { get; set; }
        public string FlowName { get; set; }
        public string Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        /// <summary>flowrun.parentrunid: parent Logic Apps run name (string), not a GUID.</summary>
        public string ParentRunName { get; set; }
        /// <summary>flowrun.callingproductrunid: also used to match parent/child runs.</summary>
        public string CallingProductRunId { get; set; }
        public string CallingProductResourceId { get; set; }
        /// <summary>When set, groups the whole parent→child run chain.</summary>
        public string ClientTrackingId { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string TriggerType { get; set; }
        public int? IsPrimary { get; set; }
        public string ResourceId { get; set; }
    }

    public sealed class FlowRunTreeJsonNode
    {
        public Guid RunId { get; set; }
        public string RunName { get; set; }
        public string FlowName { get; set; }
        public Guid WorkflowId { get; set; }
        public string Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public double? DurationMs { get; set; }
        public string ParentRunName { get; set; }
        public string CallingProductRunId { get; set; }
        public string ClientTrackingId { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string TriggerType { get; set; }
        public int? IsPrimary { get; set; }
        public string ResourceId { get; set; }
        public List<FlowRunTreeJsonNode> Children { get; set; } = new List<FlowRunTreeJsonNode>();
    }

    public sealed class FlowRunFilter
    {
        public Guid[] WorkflowIds { get; set; } = Array.Empty<Guid>();
        public FlowRunStatus Status { get; set; } = FlowRunStatus.All;
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
        public int Top { get; set; } = 100;
        /// <summary>Searches flowrunid, run name, and/or workflowid.</summary>
        public string RunIdText { get; set; }
    }

    public sealed class FlowDefinition
    {
        public Guid WorkflowId { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }

        public override string ToString() => Name ?? WorkflowId.ToString();
    }

    public sealed class SolutionDefinition
    {
        public Guid SolutionId { get; set; }
        public string FriendlyName { get; set; }
        public string UniqueName { get; set; }
        public string Version { get; set; }
        public bool IsManaged { get; set; }

        public override string ToString()
        {
            if (SolutionId == Guid.Empty)
            {
                return FriendlyName ?? "(All solutions)";
            }

            var managed = IsManaged ? "managed" : "unmanaged";
            return $"{FriendlyName} ({UniqueName}) [{managed}]";
        }
    }
}
