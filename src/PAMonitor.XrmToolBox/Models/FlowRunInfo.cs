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
        /// <summary>Logic App run id (campo name). Usado para enlazar hijos vía parentrunid.</summary>
        public string RunName { get; set; }
        public Guid WorkflowId { get; set; }
        public string FlowName { get; set; }
        public string Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        /// <summary>name del run padre (string), no GUID.</summary>
        public string ParentRunName { get; set; }
        /// <summary>Run id del caller (alternativa/complemento a parentrunid).</summary>
        public string CallingProductRunId { get; set; }
        public string CallingProductResourceId { get; set; }
        /// <summary>Agrupa todas las ejecuciones de una misma cadena padre→hijos.</summary>
        public string ClientTrackingId { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string TriggerType { get; set; }
        public int? IsPrimary { get; set; }
        public string ResourceId { get; set; }
        public IReadOnlyList<FlowActionInfo> Actions { get; set; }
        public string DetailedErrorMessage { get; set; }
    }

    public sealed class FlowActionInfo
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string Code { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public sealed class FlowRunFilter
    {
        public Guid[] WorkflowIds { get; set; } = Array.Empty<Guid>();
        public FlowRunStatus Status { get; set; } = FlowRunStatus.All;
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
        public int Top { get; set; } = 100;
        /// <summary>Matches flowrunid, Logic Apps run name, and/or workflowid.</summary>
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
