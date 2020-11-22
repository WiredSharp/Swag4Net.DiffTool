using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.OpenApi.Any;
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
        public void i_can_compare_parameter_boolean_fields(OpenApiParameter previous, OpenApiParameter actual,
            int expectedDiff)
        {
            const string parameterName = "parameter01";
            var context = ComparisonContext.FromPath("/path") with { Request = parameterName };
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
            [Values(null, ParameterLocation.Cookie, ParameterLocation.Header, ParameterLocation.Path,
                ParameterLocation.Query)]
            ParameterLocation? previous
            , [Values(null, ParameterLocation.Cookie, ParameterLocation.Header, ParameterLocation.Path,
                ParameterLocation.Query)]
            ParameterLocation? actual)
        {
            const string parameterName = "parameter01";
            var context = ComparisonContext.FromPath("/path") with { Request = parameterName };
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
        public void i_can_compare_operation_with_body_mediatypes_added_or_removed()
        {
            var context = ComparisonContext.FromPath("/path") with { Method = OperationType.Get };
            var previousOperation = new OpenApiOperation()
            {
                RequestBody = new OpenApiRequestBody()
                {
                    Content = {["application/json"] = new OpenApiMediaType()}
                }
            };
            var actualOperation = new OpenApiOperation()
            {
                RequestBody = new OpenApiRequestBody()
                {
                    Content =
                    {
                        ["application/xml"] = new OpenApiMediaType(), ["text/xml"] = new OpenApiMediaType()
                    }
                }
            };
            var diffs = previousOperation.CompareTo(actualOperation, context).ToArray();
            Assert.AreEqual(2, diffs.Length, "unexpected differences count");
            foreach (DiffResult diff in diffs)
            {
                Assert.AreEqual(context with { Request = "<Body>"}, diff.Context, "unexpected context");
                TestContext.Progress.WriteLine($"[{diff.Context}] {diff.Kind}: '{diff.Message}'");
            }
        }

        [Test]
        public void i_can_compare_operation_body()
        {
            var context = ComparisonContext.FromPath("/path") with { Method = OperationType.Get };
            var previousOperation = new OpenApiOperation()
            {
                RequestBody = new OpenApiRequestBody()
                {
                    Content =
                    {
                        ["application/json"] = new OpenApiMediaType()
                        {
                            Encoding =
                            {
                                ["ENC1"] = new OpenApiEncoding()
                                {
                                    AllowReserved = false,
                                    Headers =
                                    {
                                        ["Header1"] = new OpenApiHeader()
                                        {
                                            Required = true
                                        }
                                    }
                                }
                            },
                            Schema = new OpenApiSchema() {Type = "string"}
                        }
                    }
                }
            };
            var actualOperation = new OpenApiOperation()
            {
                RequestBody = new OpenApiRequestBody()
                {
                    Content =
                    {
                        ["application/json"] = new OpenApiMediaType()
                        {
                            Encoding =
                            {
                                ["ENC1"] = new OpenApiEncoding()
                                {
                                    AllowReserved = true,
                                    Headers =
                                    {
                                        ["Header1"] = new OpenApiHeader()
                                        {
                                            Required = false
                                        },
                                        ["Header2"] = new OpenApiHeader()
                                    }
                                }
                            }
                        },
                        ["application/xml"] = new OpenApiMediaType(), ["text/xml"] = new OpenApiMediaType()
                    }
                }
            };
            var diffs = previousOperation.CompareTo(actualOperation, context).ToArray();
            foreach (DiffResult diff in diffs)
            {
                TestContext.Progress.WriteLine($"[{diff.Context}] {diff.Kind}: '{diff.Message}'");
            }

            Assert.AreEqual(6, diffs.Length, "unexpected differences count");
            foreach (DiffResult diff in diffs)
            {
                //Assert.AreEqual(context with { Request = "<Body>"}, diff.Context, "unexpected context");
            }
        }

        [Test]
        public void i_can_compare_operation_parameter()
        {
            var context = ComparisonContext.FromPath("/path") with { Method = OperationType.Get };
            var previousOperation = new OpenApiOperation()
            {
                Parameters =
                {
                    new OpenApiParameter() {Name = "RemovedParam"},
                    new OpenApiParameter() {Name = "ModifiedParam", Deprecated = false},
                    new OpenApiParameter() {Name = "Unmodified"}
                }
            };
            var actualOperation = new OpenApiOperation()
            {
                Parameters =
                {
                    new OpenApiParameter() {Name = "AddedParam"},
                    new OpenApiParameter() {Name = "ModifiedParam", Deprecated = true},
                    new OpenApiParameter() {Name = "Unmodified"}
                }
            };
            var diffs = previousOperation.CompareTo(actualOperation, context).ToArray();
            foreach (DiffResult diff in diffs)
            {
                TestContext.Progress.WriteLine($"[{diff.Context}] {diff.Kind}: '{diff.Message}'");
            }

            Assert.AreEqual(3, diffs.Length, "unexpected differences count");
            foreach (DiffResult diff in diffs)
            {
                //Assert.AreEqual(context with { Request = "<Body>"}, diff.Context, "unexpected context");
            }
        }

        [Test]
        [TestCaseSource(nameof(Schemas))]
        public void i_can_compare_response_schema(OpenApiSchema previousSchema, OpenApiSchema actualSchema,
            int expectedDiff)
        {
            var context = ComparisonContext.FromPath("/path") with { Method = OperationType.Get };
            var previousOperation = new OpenApiOperation()
            {
                Responses = new OpenApiResponses()
                {
                    ["200"] = new OpenApiResponse()
                    {
                        Content =
                        {
                            ["application/json"] = new OpenApiMediaType()
                            {
                                Schema = previousSchema
                            }
                        }
                    }
                }
            };
            var actualOperation = new OpenApiOperation()
            {
                Responses = new OpenApiResponses()
                {
                    ["200"] = new OpenApiResponse()
                    {
                        Content =
                        {
                            ["application/json"] = new OpenApiMediaType()
                            {
                                Schema = actualSchema
                            }
                        }
                    }
                }
            };
            DiffResult[] diffs = previousOperation.CompareTo(actualOperation, context).ToArray();
            foreach (DiffResult diff in diffs)
            {
                TestContext.Progress.WriteLine($"[{diff.Context}] {diff.Kind}: '{diff.Message}'");
            }

            Assert.AreEqual(expectedDiff, diffs.Length, "unexpected differences count");
            foreach (DiffResult diff in diffs)
            {
                //Assert.AreEqual(context with { Request = "<Body>"}, diff.Context, "unexpected context");
            }
        }
        
        private static IEnumerable<TestCaseData> Parameters
        {
            get
            {
                yield return new TestCaseData(
                    new OpenApiParameter() {Deprecated = true},
                    new OpenApiParameter() {Deprecated = true}, 0) {TestName = "Deprecated still true"};
                yield return new TestCaseData(
                    new OpenApiParameter() {Deprecated = false},
                    new OpenApiParameter() {Deprecated = false}, 0) {TestName = "Deprecated still false"};
                yield return new TestCaseData(
                    new OpenApiParameter() {Deprecated = true},
                    new OpenApiParameter() {Deprecated = false}, 1) {TestName = "Deprecated switched off"};
                yield return new TestCaseData(
                    new OpenApiParameter() {Deprecated = false},
                    new OpenApiParameter() {Deprecated = true}, 1) {TestName = "Deprecated switched on"};
                yield return new TestCaseData(
                        new OpenApiParameter() {Deprecated = true, Required = true},
                        new OpenApiParameter() {Deprecated = true, Required = true}, 0)
                    {TestName = "Deprecated and required not changed"};
                yield return new TestCaseData(
                        new OpenApiParameter() {Deprecated = false, Required = false},
                        new OpenApiParameter() {Deprecated = false, Required = true}, 1)
                    {TestName = "Deprecated unchanged and Required switched on"};
                yield return new TestCaseData(
                        new OpenApiParameter() {Deprecated = true, Required = true},
                        new OpenApiParameter() {Deprecated = false, Required = false}, 2)
                    {TestName = "Both deprecated and required modified"};
                yield return new TestCaseData(
                        new OpenApiParameter() {Deprecated = true, Required = true, AllowReserved = false},
                        new OpenApiParameter() {Deprecated = false, Required = false, AllowReserved = true}, 3)
                    {TestName = "deprecated, required, and allowedReserved modified"};
            }
        }

        private static IEnumerable<TestCaseData> Schemas
        {
            get
            {
                yield return new TestCaseData(null, null, 0) {TestName = "no schema"};
                yield return new TestCaseData(null, new OpenApiSchema(), 1) {TestName = "schema added"};
                yield return new TestCaseData(new OpenApiSchema(), null, 1) {TestName = "schema removed"};
                yield return new TestCaseData(
                        new OpenApiSchema() {Type = "string", Format = "oldFormat"}
                        , new OpenApiSchema() {Type = "string", Format = "newFormat"}, 1)
                    {TestName = "string format modified"};
                yield return new TestCaseData(new OpenApiSchema() {Type = "string"},
                        new OpenApiSchema() {Type = "object"}, 1)
                    {TestName = "schema type changed"};
                yield return new TestCaseData(new OpenApiSchema() {Type = "string", Maximum = 100},
                        new OpenApiSchema() {Type = "string"}, 1)
                    {TestName = "schema maximum removed"};
                yield return new TestCaseData(new OpenApiSchema() {Type = "string"},
                        new OpenApiSchema() {Type = "string", Pattern = "a pattern"}, 1)
                    {TestName = "schema pattern added"};
                yield return new TestCaseData(new OpenApiSchema() {Type = "object"},
                        new OpenApiSchema() {Type = "object", Required = { "AddedRequiredProp" } }, 1)
                    {TestName = "required property added"};
                yield return new TestCaseData(new OpenApiSchema() {Type = "object", Required = { "RemovedRequiredProp" }},
                        new OpenApiSchema() {Type = "object" }, 1)
                    {TestName = "required property removed"};
                yield return new TestCaseData(new OpenApiSchema() {Type = "object", Required = { "RemovedRequiredProp" }},
                        new OpenApiSchema() {Type = "object", Required = { "AddedRequiredProp" } }, 2)
                    {TestName = "both required property removed and added"};
                yield return new TestCaseData(
                        new OpenApiSchema() {Type = "object", Not = new OpenApiSchema() }
                        , new OpenApiSchema() {Type = "object" }, 1)
                    {TestName = "Not schema constraint removed"};
                yield return new TestCaseData(
                        new OpenApiSchema() {Type = "object" }
                        , new OpenApiSchema() {Type = "object", Not = new OpenApiSchema() }, 1)
                    {TestName = "Not schema constraint added"};
                yield return new TestCaseData(
                        new OpenApiSchema() {Type = "string" }
                        , new OpenApiSchema() {Type = "string", Enum = { new OpenApiString("e1"),new OpenApiString("e2") } }, 2)
                    {TestName = "Enum values constraint added"};
                yield return new TestCaseData(
                        new OpenApiSchema() {Type = "string", Enum = { new OpenApiString("e1"),new OpenApiString("e2") } }
                        , new OpenApiSchema() {Type = "string" }, 2)
                    {TestName = "Enum values constraint removed"};
                yield return new TestCaseData(
                        new OpenApiSchema() {Type = "string", Enum = { new OpenApiString("e1") } }
                        , new OpenApiSchema() {Type = "string", Enum = { new OpenApiString("e1"), new OpenApiString("e2") } }, 1)
                    {TestName = "Enum value added to constraint"};
                yield return new TestCaseData(
                        new OpenApiSchema() {Type = "string", Enum = { new OpenApiString("e1"), new OpenApiString("e2") } }
                        , new OpenApiSchema() {Type = "string", Enum = { new OpenApiString("e1") } }, 1)
                    {TestName = "Enum string value removed from constraint"};
                yield return new TestCaseData(
                        new OpenApiSchema() {Type = "integer", Enum = { new OpenApiInteger(1), new OpenApiInteger(2) } }
                        , new OpenApiSchema() {Type = "integer", Enum = { new OpenApiInteger(1) } }, 1)
                    {TestName = "Enum integer value removed from constraint"};
                yield return new TestCaseData(
                        new OpenApiSchema() {Type = "number", Enum = { new OpenApiDouble(1.3), new OpenApiDouble(2.1) } }
                        , new OpenApiSchema() {Type = "number", Enum = { new OpenApiDouble(1.3) } }, 1)
                    {TestName = "Enum double value removed from constraint"};
                yield return new TestCaseData(
                        new OpenApiSchema() {Type = "number", Enum = { new OpenApiDouble(1.3), new OpenApiDouble(2.1) } }
                        , new OpenApiSchema() {Type = "number", Enum = { new OpenApiDouble(2.1), new OpenApiDouble(1.3) } }, 0)
                    {TestName = "Enum double value order changed from constraint"};
            }
        }
    }
}
