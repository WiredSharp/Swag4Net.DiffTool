using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using Swag4Net.DiffTool.Client.Helpers;

namespace Swag4Net.DiffTool.Client
{
    public static class ApiComparer
    {
        public static IEnumerable<DiffResult> CompareTo(this OpenApiDocument previous, OpenApiDocument actual)
        {
            foreach (DiffResult diffResult in CompareTo(previous.Paths, actual.Paths))
            {
                yield return diffResult;
            }
        }

        public static IEnumerable<DiffResult> CompareTo(this OpenApiPaths previous, OpenApiPaths actual) 
            => previous.CompareTo(actual, ComparisonContext.FromPath, CompareTo);

        public static IEnumerable<DiffResult> CompareTo(this OpenApiPathItem previous, OpenApiPathItem actual,
            ComparisonContext context)
        {
            if (previous.Parameters.Any() && actual.Parameters.Any())
                // shared parameters should be added to each operation parameters comparison
                throw new NotSupportedException("route shared parameters is not supported");
            return previous.Operations.CompareTo(actual.Operations
                , (k1, k2) => (int)k1 - (int)k2
                , CompareTo
                , method => context with { Method = method });
        }

        public static IEnumerable<DiffResult> CompareTo(this OpenApiOperation previous, OpenApiOperation actual,
            ComparisonContext context)
        {
            foreach (var diff in previous.Parameters.ToDictionary(p => p.Name)
                .CompareTo(actual.Parameters.ToDictionary(p => p.Name)
                    , name => context with { Parameter = name }, CompareTo))
            {
                yield return diff;
            }
			
            foreach (var diff in CompareTo(previous.RequestBody, actual.RequestBody, context with { Parameter = "<Body>" }))
            {
                yield return diff;
            }

            DiffResult? comparison = CompareBool(previous.Deprecated, actual.Deprecated, context, "operation is no more deprecated", "operation is now deprecated");
            if (comparison != null)
                yield return comparison;

            previous.Responses.CompareTo(actual.Responses, _ => context, CompareTo);
        }

        public static IEnumerable<DiffResult> CompareTo(this OpenApiResponse previous, OpenApiResponse actual,
            ComparisonContext context)
        {
            throw new NotImplementedException("api response comparison still under construction...");
        }

        public static IEnumerable<DiffResult> CompareTo(this OpenApiRequestBody previous, OpenApiRequestBody actual,
            ComparisonContext context)
        {
            foreach (var diff in previous.Content.CompareTo(actual.Content, _ => context, CompareTo))
            {
                yield return diff;
            }
            DiffResult? comparison = CompareBool(previous.Required, actual.Required, context, "request body is no more required", "request body is now required");
            if (comparison != null)
                yield return comparison;
        }

        public static IEnumerable<DiffResult> CompareTo(this OpenApiParameter previous, OpenApiParameter actual,
            ComparisonContext context)
        {
            foreach (var diff in previous.Content.CompareTo(actual.Content, _ => context, CompareTo))
            {
                yield return diff;
            }

            DiffResult? comparison = CompareBool(previous.Deprecated, actual.Deprecated, context, "parameter is no more deprecated", "parameter is now deprecated");
            if (comparison != null)
                yield return comparison;
            
            comparison = CompareBool(previous.Required, actual.Required, context, "parameter is no more required", "parameter is now required");
            if (comparison != null)
                yield return comparison;
            
            comparison = CompareBool(previous.AllowReserved, actual.AllowReserved, context, "parameter does not allow reserved characters any more", "parameter now allows reserved characters");
            if (comparison != null)
                yield return comparison;

            comparison = CompareBool(previous.Explode, actual.Explode, context, "parameter cannot be exploded any more", "parameter now allows to be exploded");
            if (comparison != null)
                yield return comparison;

            if (previous.Style != actual.Style)
            {
                yield return new DiffResult(DifferenceKind.Modified, context,
                    $"parameter style has changed from { previous.Style.ToStringOrDefault("None") } to { actual.Style.ToStringOrDefault("None") }");
            }

            if (previous.In != actual.In)
            {
                yield return new DiffResult(DifferenceKind.Modified, context,
                    $"parameter position has changed from {previous.In.ToStringOrDefault("None")} to {actual.In.ToStringOrDefault("None")}");
            }
        }

        private static IEnumerable<DiffResult> CompareTo(OpenApiMediaType previous, OpenApiMediaType actual,
            ComparisonContext context)
        {
            throw new NotImplementedException("api media type comparison still under construction...");
        }

        private static DiffResult? CompareBool(bool previous, bool actual, ComparisonContext context, string switchOffMessage, string switchOnMessage)
        {
            switch (previous, actual)
            {
                case (true, false):
                    return new DiffResult(DifferenceKind.Modified, context, switchOffMessage);
                case (false, true):
                    return new DiffResult(DifferenceKind.Modified, context, switchOnMessage);
                default:
                    return null;
            }
        }
    }
}