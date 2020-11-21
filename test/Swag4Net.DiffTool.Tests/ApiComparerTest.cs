using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using Swag4Net.DiffTool.Client;
using Swag4Net.DiffTool.Tests.TestHelpers;

namespace Swag4Net.DiffTool.Tests
{
    [TestFixture]
    public class ApiComparerTest
    {
        [Test]
        [TestCaseSource(nameof(Parameters))]
        public void i_can_compare_parameter_boolean_fields(OpenApiParameter previous, OpenApiParameter actual, int expectedDiff)
        {
            const string parameterName = "parameter01";
            var context = ComparisonContext.FromPath("/path") with { Parameter = parameterName };
            var diffs = previous.CompareTo(actual, context).ToArray();
            Assert.AreEqual(expectedDiff, diffs.Length, "unexpected differences count");
            foreach (DiffResult diff in diffs)
            {
                Assert.AreEqual(context, diff.Context, "unexpected context");
                TestContext.Progress.WriteLine($"[{diff.Context}] {diff.Kind}: '{diff.Message}'");
            }
        }

        [Test]
        public void i_can_compare_parameter_location_field(
            [Values(null, ParameterLocation.Cookie, ParameterLocation.Header, ParameterLocation.Path, ParameterLocation.Query)]ParameterLocation? previous
            , [Values(null, ParameterLocation.Cookie, ParameterLocation.Header, ParameterLocation.Path, ParameterLocation.Query)]ParameterLocation? actual)
        {
            const string parameterName = "parameter01";
            var context = ComparisonContext.FromPath("/path") with { Parameter = parameterName };
            var previousParameter = new OpenApiParameter() {In = previous};
            var actualParameter = new OpenApiParameter() {In = actual};
            var diffs = previousParameter.CompareTo(actualParameter, context).ToArray();
            Assert.AreEqual(previous == actual ? 0 : 1, diffs.Length, "unexpected differences count");
            foreach (DiffResult diff in diffs)
            {
                Assert.AreEqual(context, diff.Context, "unexpected context");
                TestContext.Progress.WriteLine($"[{diff.Context}] {diff.Kind}: '{diff.Message}'");
            }
        }

        
        [Test]
        public void i_can_compare_operation_with_body()
        {
            var context = ComparisonContext.FromPath("/path") with { Method = OperationType.Get };
            var previousOperation = new OpenApiOperation() { RequestBody = new OpenApiRequestBody()
            {
                Content = { ["application/json"] = new OpenApiMediaType() }
            } };
            var actualOperation = new OpenApiOperation() { RequestBody = new OpenApiRequestBody()
            {
                Content =
                {
                    ["application/xml"] = new OpenApiMediaType()
                    ,["text/xml"] = new OpenApiMediaType()
                }
            } };
            var diffs = previousOperation.CompareTo(actualOperation, context).ToArray();
            Assert.AreEqual(2, diffs.Length, "unexpected differences count");
            foreach (DiffResult diff in diffs)
            {
                Assert.AreEqual(context with { Parameter = "<Body>"}, diff.Context, "unexpected context");
                TestContext.Progress.WriteLine($"[{diff.Context}] {diff.Kind}: '{diff.Message}'");
            }
        }

        private static IEnumerable<TestCaseData> Parameters
        {
            get
            {
                yield return new TestCaseData(
                    new OpenApiParameter() {Deprecated = true},
                    new OpenApiParameter() {Deprecated = true}, 0) { TestName = "Deprecated still true" };
                yield return new TestCaseData(
                    new OpenApiParameter() {Deprecated = false},
                    new OpenApiParameter() {Deprecated = false}, 0) { TestName = "Deprecated still false" };
                yield return new TestCaseData(
                    new OpenApiParameter() {Deprecated = true},
                    new OpenApiParameter() {Deprecated = false}, 1) { TestName = "Deprecated switched off" };
                yield return new TestCaseData(
                    new OpenApiParameter() {Deprecated = false},
                    new OpenApiParameter() {Deprecated = true}, 1) { TestName = "Deprecated switched on" };
                yield return new TestCaseData(
                    new OpenApiParameter() {Deprecated = true, Required = true},
                    new OpenApiParameter() {Deprecated = true, Required = true}, 0) { TestName = "Deprecated and required not changed" };
                yield return new TestCaseData(
                    new OpenApiParameter() {Deprecated = false, Required = false},
                    new OpenApiParameter() {Deprecated = false, Required = true}, 1) { TestName = "Deprecated unchanged and Required switched on" };
                yield return new TestCaseData(
                    new OpenApiParameter() {Deprecated = true, Required = true},
                    new OpenApiParameter() {Deprecated = false, Required = false}, 2) { TestName = "Both deprecated and required modified" };
                yield return new TestCaseData(
                    new OpenApiParameter() {Deprecated = true, Required = true, AllowReserved = false},
                    new OpenApiParameter() {Deprecated = false, Required = false, AllowReserved = true}, 3) { TestName = "deprecated, required, and allowedReserved modified" };
            }
        }
    }
}