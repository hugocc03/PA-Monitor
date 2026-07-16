using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using PAMonitor.XrmToolBox.Models;

namespace PAMonitor.XrmToolBox.Services
{
    /// <summary>
    /// Consultas contra Dataverse para listar flujos y ejecuciones.
    /// Esquema flowrun: workflowid/parentrunid son string (no lookup).
    /// parentrunid apunta al name del run padre (Logic App run id), no al flowrunid GUID.
    /// Nota: NO usar early-bound (conflicto entre tools de XrmToolBox).
    /// </summary>
    public sealed class FlowRunQueryService
    {
        private readonly IOrganizationService _service;

        public FlowRunQueryService(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Visible solutions (excludes Default/Active). Used to optionally filter flows.
        /// </summary>
        public IReadOnlyList<SolutionDefinition> GetSolutions()
        {
            var query = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("solutionid", "friendlyname", "uniquename", "version", "ismanaged"),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("isvisible", ConditionOperator.Equal, true),
                        new ConditionExpression("uniquename", ConditionOperator.NotEqual, "Default"),
                        new ConditionExpression("uniquename", ConditionOperator.NotEqual, "Active")
                    }
                },
                Orders = { new OrderExpression("friendlyname", OrderType.Ascending) }
            };

            var results = _service.RetrieveMultiple(query);
            return results.Entities
                .Select(e => new SolutionDefinition
                {
                    SolutionId = e.Id,
                    FriendlyName = e.GetAttributeValue<string>("friendlyname"),
                    UniqueName = e.GetAttributeValue<string>("uniquename"),
                    Version = e.GetAttributeValue<string>("version"),
                    IsManaged = e.GetAttributeValue<bool>("ismanaged")
                })
                .ToList();
        }

        /// <param name="solutionIds">
        /// When provided, only flows (component type Workflow = 29) included in any of these solutions.
        /// </param>
        public IReadOnlyList<FlowDefinition> GetCloudFlows(IEnumerable<Guid> solutionIds = null)
        {
            var query = new QueryExpression("workflow")
            {
                Distinct = true,
                ColumnSet = new ColumnSet("workflowid", "name", "category", "type", "statecode"),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("type", ConditionOperator.Equal, 1),
                        new ConditionExpression("statecode", ConditionOperator.Equal, 1)
                    }
                },
                Orders = { new OrderExpression("name", OrderType.Ascending) }
            };

            query.Criteria.AddCondition("category", ConditionOperator.Equal, 5);

            var ids = solutionIds?.Where(id => id != Guid.Empty).Distinct().Cast<object>().ToArray();
            if (ids != null && ids.Length > 0)
            {
                var link = query.AddLink("solutioncomponent", "workflowid", "objectid");
                link.EntityAlias = "sc";
                link.LinkCriteria.AddCondition("solutionid", ConditionOperator.In, ids);
                // 29 = Workflow (includes modern cloud flows)
                link.LinkCriteria.AddCondition("componenttype", ConditionOperator.Equal, 29);
            }

            var results = _service.RetrieveMultiple(query);
            return results.Entities
                .Select(e => new FlowDefinition
                {
                    WorkflowId = e.Id,
                    Name = e.GetAttributeValue<string>("name"),
                    Category = "Modern Flow"
                })
                .OrderBy(f => f.Name)
                .ToList();
        }

        public IReadOnlyList<FlowRunInfo> GetRuns(FlowRunFilter filter)
        {
            filter = filter ?? new FlowRunFilter();

            var query = new QueryExpression("flowrun")
            {
                ColumnSet = CreateRunColumnSet(),
                TopCount = filter.Top > 0 ? filter.Top : 100,
                Orders = { new OrderExpression("starttime", OrderType.Descending) }
            };

            var criteria = query.Criteria;

            if (filter.WorkflowIds != null && filter.WorkflowIds.Length > 0)
            {
                // workflowid es string en flowrun
                var ids = filter.WorkflowIds.Select(id => (object)id.ToString("D")).ToArray();
                criteria.AddCondition("workflowid", ConditionOperator.In, ids);
            }

            if (filter.FromUtc.HasValue)
            {
                criteria.AddCondition("starttime", ConditionOperator.GreaterEqual, filter.FromUtc.Value);
            }

            if (filter.ToUtc.HasValue)
            {
                criteria.AddCondition("starttime", ConditionOperator.LessEqual, filter.ToUtc.Value);
            }

            var statusValue = MapStatusFilter(filter.Status);
            if (statusValue != null)
            {
                criteria.AddCondition("status", ConditionOperator.Equal, statusValue);
            }

            if (!string.IsNullOrWhiteSpace(filter.RunIdText))
            {
                var text = filter.RunIdText.Trim();
                var runFilter = new FilterExpression(LogicalOperator.Or);
                if (Guid.TryParse(text, out var idGuid))
                {
                    runFilter.AddCondition("flowrunid", ConditionOperator.Equal, idGuid);
                    runFilter.AddCondition("workflowid", ConditionOperator.Equal, idGuid.ToString("D"));
                    runFilter.AddCondition("workflowid", ConditionOperator.Equal, idGuid.ToString("N"));
                }

                runFilter.AddCondition("name", ConditionOperator.Equal, text);
                runFilter.AddCondition("name", ConditionOperator.Like, "%" + EscapeLike(text) + "%");
                runFilter.AddCondition("workflowid", ConditionOperator.Like, "%" + EscapeLike(text) + "%");
                criteria.AddFilter(runFilter);
            }

            var results = _service.RetrieveMultiple(query);
            return results.Entities.Select(MapRun).ToList();
        }

        private static string EscapeLike(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("_", "[_]");
        }

        /// <summary>
        /// Hijos cuyo parentrunid = name del run padre (no el GUID flowrunid).
        /// </summary>
        public IReadOnlyList<FlowRunInfo> GetChildRuns(string parentRunName)
        {
            if (string.IsNullOrWhiteSpace(parentRunName))
            {
                return Array.Empty<FlowRunInfo>();
            }

            var query = new QueryExpression("flowrun")
            {
                ColumnSet = CreateRunColumnSet(),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("parentrunid", ConditionOperator.Equal, parentRunName)
                    }
                },
                Orders = { new OrderExpression("starttime", OrderType.Ascending) }
            };

            var results = _service.RetrieveMultiple(query);
            return results.Entities.Select(MapRun).ToList();
        }

        private static ColumnSet CreateRunColumnSet()
        {
            return new ColumnSet(
                "flowrunid",
                "name",
                "status",
                "starttime",
                "endtime",
                "duration",
                "workflowid",
                "workflow",
                "parentrunid",
                "errorcode",
                "errormessage",
                "triggertype",
                "isprimary",
                "resourceid");
        }

        private static FlowRunInfo MapRun(Entity e)
        {
            var start = e.GetAttributeValue<DateTime?>("starttime");
            var end = e.GetAttributeValue<DateTime?>("endtime");
            var workflowLookup = e.GetAttributeValue<EntityReference>("workflow");
            var workflowId = ParseGuid(e.GetAttributeValue<string>("workflowid"))
                             ?? workflowLookup?.Id
                             ?? Guid.Empty;

            var durationMs = ReadDurationMs(e);
            TimeSpan? duration = null;
            if (durationMs.HasValue)
            {
                duration = TimeSpan.FromMilliseconds(durationMs.Value);
            }
            else if (start.HasValue && end.HasValue)
            {
                duration = end - start;
            }

            return new FlowRunInfo
            {
                RunId = e.Id,
                RunName = e.GetAttributeValue<string>("name"),
                WorkflowId = workflowId,
                FlowName = workflowLookup?.Name,
                Status = e.GetAttributeValue<string>("status")
                         ?? (e.FormattedValues.Contains("status") ? e.FormattedValues["status"] : null),
                StartTime = start,
                EndTime = end,
                Duration = duration,
                ParentRunName = e.GetAttributeValue<string>("parentrunid"),
                ErrorCode = e.GetAttributeValue<string>("errorcode"),
                ErrorMessage = e.GetAttributeValue<string>("errormessage"),
                TriggerType = e.GetAttributeValue<string>("triggertype"),
                IsPrimary = ReadIsPrimary(e),
                ResourceId = e.GetAttributeValue<string>("resourceid")
            };
        }

        private static int? ReadIsPrimary(Entity e)
        {
            if (!e.Contains("isprimary") || e["isprimary"] == null)
            {
                return null;
            }

            var value = e["isprimary"];
            if (value is bool b)
            {
                return b ? 1 : 0;
            }

            if (value is OptionSetValue osv)
            {
                return osv.Value;
            }

            if (value is int i)
            {
                return i;
            }

            return null;
        }

        private static long? ReadDurationMs(Entity e)
        {
            if (!e.Contains("duration") || e["duration"] == null)
            {
                return null;
            }

            var value = e["duration"];
            if (value is long l)
            {
                return l;
            }

            if (value is int i)
            {
                return i;
            }

            if (value is decimal d)
            {
                return (long)d;
            }

            return null;
        }

        private static Guid? ParseGuid(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return Guid.TryParse(value, out var id) ? id : (Guid?)null;
        }

        /// <summary>
        /// Valores documentados: Succeeded / Failed / Cancelled (y variantes Success).
        /// </summary>
        private static object MapStatusFilter(FlowRunStatus status)
        {
            switch (status)
            {
                case FlowRunStatus.Succeeded:
                    return "Succeeded";
                case FlowRunStatus.Failed:
                    return "Failed";
                case FlowRunStatus.Cancelled:
                    return "Cancelled";
                case FlowRunStatus.Running:
                    return "Running";
                case FlowRunStatus.Waiting:
                    return "Waiting";
                default:
                    return null;
            }
        }
    }
}
