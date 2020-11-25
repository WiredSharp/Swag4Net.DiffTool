using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Any;
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
					  , context.AppendParameter, CompareTo))
			{
				yield return diff;
			}

			foreach (var diff in CompareTo(previous.RequestBody, actual.RequestBody,
				 context with { Request = "<Body>" }))
			{
				yield return diff;
			}

			DiffResult? comparison = ComparisonHelpers.CompareBool(previous.Deprecated, actual.Deprecated, context,
				 "operation is no more deprecated", "operation is now deprecated");
			if (comparison != null)
				yield return comparison;

			foreach (var diff in previous.Responses.CompareTo(actual.Responses, context.AppendResponse, CompareTo))
			{
				yield return diff;
			}
		}

		public static IEnumerable<DiffResult> CompareTo(this OpenApiResponse previous, OpenApiResponse actual,
			 ComparisonContext context)
		{
			foreach (var diff in previous.Content.CompareTo(actual.Content,
				 media => context.AppendResponse($"<{media}>"), CompareTo))
			{
				yield return diff;
			}

			foreach (var diff in previous.Headers.CompareTo(actual.Headers, context.AppendResponse, CompareTo))
			{
				yield return diff;
			}
		}

		public static IEnumerable<DiffResult> CompareTo(this OpenApiRequestBody? previous, OpenApiRequestBody? actual,
			 ComparisonContext context)
		{
			foreach (var diffResult in ComparisonHelpers.HandleNullValue(previous, actual, compareExisting, context))
				yield return diffResult;

			IEnumerable<DiffResult> compareExisting(OpenApiRequestBody previousRequestBody,
				 OpenApiRequestBody actualRequestBody, ComparisonContext ctx)
			{
				foreach (var diff in previousRequestBody.Content.CompareTo(actualRequestBody.Content,
					 media => ctx.AppendParameter($"<{media}>"), CompareTo))
				{
					yield return diff;
				}

				DiffResult? comparison = ComparisonHelpers.CompareBool(previousRequestBody.Required,
					 actualRequestBody.Required, ctx,
					 "request body is no more required", "request body is now required");
				if (comparison != null)
					yield return comparison;
			}
		}

		public static IEnumerable<DiffResult> CompareTo(this OpenApiParameter previous, OpenApiParameter actual,
			 ComparisonContext context)
		{
			foreach (var diff in previous.Content.CompareTo(actual.Content,
				 media => context.AppendParameter($"<{media}>"), CompareTo))
			{
				yield return diff;
			}

			DiffResult? comparison = ComparisonHelpers.CompareBool(previous.Deprecated, actual.Deprecated, context,
				 "parameter is no more deprecated", "parameter is now deprecated");
			if (comparison != null)
				yield return comparison;

			comparison = ComparisonHelpers.CompareBool(previous.Required, actual.Required, context,
				 "parameter is no more required", "parameter is now required");
			if (comparison != null)
				yield return comparison;

			comparison = ComparisonHelpers.CompareBool(previous.AllowReserved, actual.AllowReserved, context,
				 "parameter does not allow reserved characters any more", "parameter now allows reserved characters");
			if (comparison != null)
				yield return comparison;

			comparison = ComparisonHelpers.CompareBool(previous.Explode, actual.Explode, context,
				 "parameter cannot be exploded any more", "parameter now allows to be exploded");
			if (comparison != null)
				yield return comparison;

			if (previous.Style != actual.Style)
			{
				yield return new DiffResult(DifferenceKind.Modified, context,
					 $"parameter style has changed from {previous.Style.ToStringOrDefault("None")} to {actual.Style.ToStringOrDefault("None")}");
			}

			if (previous.In != actual.In)
			{
				yield return new DiffResult(DifferenceKind.Modified, context,
					 $"parameter position has changed from {previous.In.ToStringOrDefault("None")} to {actual.In.ToStringOrDefault("None")}");
			}
		}

		public static IEnumerable<DiffResult> CompareTo(OpenApiMediaType previous, OpenApiMediaType actual,
			 ComparisonContext context)
		{
			foreach (var diff in previous.Encoding.CompareTo(actual.Encoding,
				 encoding => context.AppendParameter($"<{encoding}>"), CompareTo))
			{
				yield return diff;
			}

			foreach (var diff in previous.Schema.CompareTo(actual.Schema, context))
			{
				yield return diff;
			}
		}

		public static IEnumerable<DiffResult> CompareTo(this OpenApiEncoding previous, OpenApiEncoding actual,
			 ComparisonContext context)
		{
			foreach (var diff in previous.Headers.CompareTo(actual.Headers, context.AppendParameter, CompareTo))
			{
				yield return diff;
			}

			if (previous.Style != actual.Style)
			{
				yield return new DiffResult(DifferenceKind.Modified, context,
					 $"style has changed from {previous.Style.ToStringOrDefault("None")} to {actual.Style.ToStringOrDefault("None")}");
			}

			var comparison = ComparisonHelpers.CompareBool(previous.Explode, actual.Explode, context,
				 "cannot be exploded any more", " now allows to be exploded");
			if (comparison != null)
				yield return comparison;
			comparison = ComparisonHelpers.CompareBool(previous.AllowReserved, actual.AllowReserved, context,
				 "does not allow reserved characters any more", "now allows reserved characters");
			if (comparison != null)
				yield return comparison;
		}

		private static IEnumerable<DiffResult> CompareTo(OpenApiHeader previous, OpenApiHeader actual,
			 ComparisonContext context)
		{
			foreach (var diff in previous.Content.CompareTo(actual.Content,
				 media => context.AppendParameter($"<{media}>"), CompareTo))
			{
				yield return diff;
			}

			DiffResult? comparison = ComparisonHelpers.CompareBool(previous.Deprecated, actual.Deprecated, context,
				 "header is no more deprecated", "header is now deprecated");
			if (comparison != null)
				yield return comparison;

			comparison = ComparisonHelpers.CompareBool(previous.Required, actual.Required, context,
				 "header is no more required", "header is now required");
			if (comparison != null)
				yield return comparison;

			comparison = ComparisonHelpers.CompareBool(previous.AllowReserved, actual.AllowReserved, context,
				 "header does not allow reserved characters any more", "header now allows reserved characters");
			if (comparison != null)
				yield return comparison;

			comparison = ComparisonHelpers.CompareBool(previous.Explode, actual.Explode, context,
				 "header cannot be exploded any more", "header now allows to be exploded");
			if (comparison != null)
				yield return comparison;

			if (previous.Style != actual.Style)
			{
				yield return new DiffResult(DifferenceKind.Modified, context,
					 $"parameter style has changed from {previous.Style.ToStringOrDefault("None")} to {actual.Style.ToStringOrDefault("None")}");
			}

			foreach (var diff in previous.Schema.CompareTo(actual.Schema, context))
			{
				yield return diff;
			}
		}

		public static IEnumerable<DiffResult> CompareTo(this OpenApiSchema? previous, OpenApiSchema? actual,
			 ComparisonContext context)
		{
			return ComparisonHelpers.HandleNullValue(previous, actual, compareExisting, context);

			IEnumerable<DiffResult> compareExisting(OpenApiSchema previousSchema,
				 OpenApiSchema actualSchema, ComparisonContext ctx)
			{
				DiffResult? comparison = ComparisonHelpers.CompareBool(previousSchema.Deprecated,
					 actualSchema.Deprecated, context.AppendSchema(nameof(OpenApiSchema.Deprecated)),
					 "schema is no more deprecated", "schema is now deprecated");
				if (comparison != null)
					yield return comparison;
				comparison = ComparisonHelpers.CompareBool(previousSchema.Nullable, actualSchema.Nullable,
					 context.AppendSchema(nameof(OpenApiSchema.Nullable)), "schema does not allow null any more",
					 "schema allows null value from now");
				if (comparison != null)
					yield return comparison;
				comparison = ComparisonHelpers.CompareBool(previousSchema.ExclusiveMaximum,
					 actualSchema.ExclusiveMaximum, context.AppendSchema(nameof(OpenApiSchema.ExclusiveMaximum)),
					 "schema max value is now exclusive", "schema max value is no more exclusive");
				if (comparison != null)
					yield return comparison;
				comparison = ComparisonHelpers.CompareBool(previousSchema.ExclusiveMinimum,
					 actualSchema.ExclusiveMinimum, context.AppendSchema(nameof(OpenApiSchema.ExclusiveMinimum)),
					 "schema min value is now exclusive", "schema min value is no more exclusive");
				if (comparison != null)
					yield return comparison;
				comparison = ComparisonHelpers.CompareBool(previousSchema.ReadOnly, actualSchema.ReadOnly,
					 context.AppendSchema(nameof(OpenApiSchema.ReadOnly)), "value is now readonly",
					 "value is not readonly any more");
				if (comparison != null)
					yield return comparison;
				comparison = ComparisonHelpers.CompareBool(previousSchema.UniqueItems, actualSchema.UniqueItems,
					 context.AppendSchema(nameof(OpenApiSchema.UniqueItems)), "all elements are now unique",
					 "element are not unique any more");
				if (comparison != null)
					yield return comparison;
				comparison = ComparisonHelpers.CompareBool(previousSchema.WriteOnly, actualSchema.WriteOnly,
					 context.AppendSchema(nameof(OpenApiSchema.WriteOnly)), "value is now writeonly",
					 "value is not writeonly any more");
				if (comparison != null)
					yield return comparison;
				comparison = ComparisonHelpers.CompareBool(previousSchema.AdditionalPropertiesAllowed,
					 actualSchema.AdditionalPropertiesAllowed,
					 context.AppendSchema(nameof(OpenApiSchema.AdditionalPropertiesAllowed)),
					 "additional properties are now allowed", "additional properties are not allowed any more");
				if (comparison != null)
					yield return comparison;
				foreach (DiffResult diff in ComparisonHelpers.CompareScalar(previousSchema.Format, actualSchema.Format,
					 ctx.AppendSchema(nameof(OpenApiSchema.Format)), "schema format has changed"))
				{
					yield return diff;
				}

				foreach (DiffResult diff in ComparisonHelpers.CompareScalar(previousSchema.Maximum,
					 actualSchema.Maximum, ctx.AppendSchema(nameof(OpenApiSchema.Maximum)),
					 "schema maximum value has changed"))
				{
					yield return diff;
				}

				foreach (DiffResult diff in ComparisonHelpers.CompareScalar(previousSchema.Minimum,
					 actualSchema.Minimum, ctx.AppendSchema(nameof(OpenApiSchema.Minimum)),
					 "schema minimum value has changed"))
				{
					yield return diff;
				}

				foreach (DiffResult diff in ComparisonHelpers.CompareScalar(previousSchema.Pattern,
					 actualSchema.Pattern, ctx.AppendSchema(nameof(OpenApiSchema.Pattern)),
					 "schema pattern has changed"))
				{
					yield return diff;
				}

				foreach (DiffResult diff in ComparisonHelpers.CompareScalar(previousSchema.Type, actualSchema.Type,
					 ctx.AppendSchema(nameof(OpenApiSchema.Type)), "schema type has changed"))
				{
					yield return diff;
				}

				foreach (DiffResult diff in ComparisonHelpers.CompareScalar(previousSchema.MaxItems,
					 actualSchema.MaxItems, ctx.AppendSchema(nameof(OpenApiSchema.MaxItems)),
					 "array max items has changed"))
				{
					yield return diff;
				}

				foreach (DiffResult diff in ComparisonHelpers.CompareScalar(previousSchema.MinItems,
					 actualSchema.MinItems, ctx.AppendSchema(nameof(OpenApiSchema.MinItems)),
					 "array min items has changed"))
				{
					yield return diff;
				}

				foreach (DiffResult diff in ComparisonHelpers.CompareScalar(previousSchema.MaxLength,
					 actualSchema.MaxLength, ctx.AppendSchema(nameof(OpenApiSchema.MaxLength)),
					 " max length has changed"))
				{
					yield return diff;
				}

				foreach (DiffResult diff in ComparisonHelpers.CompareScalar(previousSchema.MinLength,
					 actualSchema.MinLength, ctx.AppendSchema(nameof(OpenApiSchema.MinLength)),
					 "array min length has changed"))
				{
					yield return diff;
				}

				foreach (DiffResult diff in ComparisonHelpers.CompareScalar(previousSchema.MinProperties,
					 actualSchema.MinProperties, ctx.AppendSchema(nameof(OpenApiSchema.MinProperties)),
					 "min properties has changed"))
				{
					yield return diff;
				}

				foreach (DiffResult diff in ComparisonHelpers.CompareScalar(previousSchema.MultipleOf,
					 actualSchema.MultipleOf, ctx.AppendSchema(nameof(OpenApiSchema.MultipleOf)),
					 "value 'multiple of' has changed"))
				{
					yield return diff;
				}

				foreach (DiffResult diff in CompareTo(previousSchema.Discriminator, actualSchema.Discriminator,
					 ctx.AppendSchema(nameof(OpenApiSchema.Discriminator))))
				{
					yield return diff;
				}

				foreach (DiffResult diff in previousSchema.Required.ToDictionary(x => x)
					 .CompareTo(actualSchema.Required.ToDictionary(x => x),
						  p => ctx.AppendSchema(nameof(OpenApiSchema.Required)), ComparisonHelpers.NopCompare))
				{
					yield return diff;
				}

				foreach (DiffResult diff in previousSchema.Not.CompareTo(actualSchema.Not,
					 ctx.AppendSchema(nameof(OpenApiSchema.Not))))
				{
					yield return diff;
				}

				foreach (DiffResult diff in CompareEnum(previousSchema.Enum, actualSchema.Enum,
					 ctx.AppendSchema(nameof(OpenApiSchema.Enum))))
				{
					yield return diff;
				}
				var newCtx = ctx.AppendSchema(nameof(OpenApiSchema.OneOf));
				foreach (DiffResult diff in previousSchema.OneOf.CompareTo(actualSchema.OneOf, s => newCtx))
				{
					yield return diff;
				}
				newCtx = ctx.AppendSchema(nameof(OpenApiSchema.AllOf));
				foreach (DiffResult diff in previousSchema.AllOf.CompareTo(actualSchema.AllOf, s => newCtx))
				{
					yield return diff;
				}
				newCtx = ctx.AppendSchema(nameof(OpenApiSchema.AnyOf));
				foreach (DiffResult diff in previousSchema.AnyOf.CompareTo(actualSchema.AnyOf, s => newCtx))
				{
					yield return diff;
				}
				foreach (DiffResult diff in previousSchema.Properties.CompareTo(actualSchema.Properties, p => ctx.AppendSchema(p), CompareTo))
				{
					yield return diff;
				}
			}
		}

		private static IEnumerable<DiffResult> CompareEnum(IList<IOpenApiAny> previousEnum,
			 IList<IOpenApiAny> actualEnum
			 , ComparisonContext context)
		{
			// identify enum type for each list
			var previousSample = previousEnum.FirstOrDefault();
			var actualSample = actualEnum.FirstOrDefault();

			if (previousSample == null && actualSample == null)
			{
				yield break;
			}

			if (previousSample != null && actualSample != null && previousSample.GetType() == actualSample.GetType())
			{
				foreach (var diffResult in ToDiffResults(previousEnum, actualEnum, context))
					yield return diffResult;
				yield break;
			}

			if (previousSample != null)
			{
				foreach (var diffResult in ToDiffResults(previousEnum, DifferenceKind.Removed, context))
					yield return diffResult;
			}

			if (actualSample != null)
			{
				foreach (var diffResult in ToDiffResults(actualEnum, DifferenceKind.Added, context))
					yield return diffResult;
			}
		}

		private static IEnumerable<DiffResult> ToDiffResults(IList<IOpenApiAny> values,
			 DifferenceKind differenceKind,
			 ComparisonContext context)
		{
			switch (values.FirstOrDefault())
			{
				case null:
					break;
				case OpenApiString:
					foreach (DiffResult diff in values.Cast<OpenApiString>().Select(e =>
						 new DiffResult(differenceKind, context, $"enum value '{e.Value}'")))
					{
						yield return diff;
					}

					break;
				case OpenApiInteger:
					foreach (DiffResult diff in values.Cast<OpenApiInteger>().Select(e =>
						 new DiffResult(differenceKind, context, $"enum value '{e.Value}'")))
					{
						yield return diff;
					}

					break;
				case OpenApiLong:
					foreach (DiffResult diff in values.Cast<OpenApiLong>().Select(e =>
						 new DiffResult(differenceKind, context, $"enum value '{e.Value}'")))
					{
						yield return diff;
					}

					break;
				case OpenApiDouble:
					foreach (DiffResult diff in values.Cast<OpenApiDouble>().Select(e =>
						 new DiffResult(differenceKind, context, $"enum value '{e.Value}'")))
					{
						yield return diff;
					}

					break;
				case OpenApiFloat:
					foreach (DiffResult diff in values.Cast<OpenApiDouble>().Select(e =>
						 new DiffResult(differenceKind, context, $"enum value '{e.Value}'")))
					{
						yield return diff;
					}

					break;
				default:
					throw new NotSupportedException($"{values.First()}: type is not supported");
			}
		}

		private static IEnumerable<DiffResult> ToDiffResults(IList<IOpenApiAny> previous,
			 IList<IOpenApiAny> actual, ComparisonContext context)
		{
			switch (previous.First())
			{
				case OpenApiString:
					foreach (DiffResult diff in previous.Cast<OpenApiString>().Select(e =>
						 e.Value).CompareTo(actual.Cast<OpenApiString>().Select(e => e.Value)
						 , _ => context, String.Compare))
					{
						yield return diff;
					}
					break;
				case OpenApiInteger:
					foreach (DiffResult diff in previous.Cast<OpenApiInteger>().Select(e =>
						 e.Value).CompareTo(actual.Cast<OpenApiInteger>().Select(e => e.Value)
						 , _ => context, (i1, i2) => i1 - i2))
					{
						yield return diff;
					}
					break;
				case OpenApiLong:
					foreach (DiffResult diff in previous.Cast<OpenApiLong>().Select(e =>
						 e.Value).CompareTo(actual.Cast<OpenApiLong>().Select(e => e.Value)
						 , _ => context, (i1, i2) => (int)(i1 - i2)))
					{
						yield return diff;
					}
					break;
				case OpenApiDouble:
					foreach (DiffResult diff in previous.Cast<OpenApiDouble>().Select(e =>
						 e.Value).CompareTo(actual.Cast<OpenApiDouble>().Select(e => e.Value)
						 , _ => context, (i1, i2) => (int)(i1 - i2)))
					{
						yield return diff;
					}
					break;
				case OpenApiFloat:
					foreach (DiffResult diff in previous.Cast<OpenApiLong>().Select(e =>
						 e.Value).CompareTo(actual.Cast<OpenApiLong>().Select(e => e.Value)
						 , _ => context, (i1, i2) => (int)(i1 - i2)))
					{
						yield return diff;
					}
					break;
				default:
					throw new NotSupportedException($"{previous.First()}: type is not supported");
			}
		}

		private static IEnumerable<DiffResult> CompareTo(OpenApiDiscriminator? previousSchemaDiscriminator,
			 OpenApiDiscriminator? actualSchemaDiscriminator, ComparisonContext context)
		{
			foreach (DiffResult diff in ComparisonHelpers.HandleNullValue(previousSchemaDiscriminator,
				 actualSchemaDiscriminator, compareExisting, context))
			{
				yield return diff;
			}

			IEnumerable<DiffResult> compareExisting(OpenApiDiscriminator previous, OpenApiDiscriminator actual,
				 ComparisonContext ctx)
			{
				foreach (DiffResult diff in ComparisonHelpers.CompareScalar(previous.PropertyName, actual.PropertyName,
					 ctx.AppendSchema(nameof(OpenApiDiscriminator.PropertyName)), "discriminator property has changed"))
				{
					yield return diff;
				}

				var discriminatorCtx = ctx.AppendSchema("discriminator");
				foreach (DiffResult diff in previous.Mapping.CompareTo(actual.Mapping, _ => discriminatorCtx,
					 (pmap, amap, c) => ComparisonHelpers.CompareScalar(pmap, amap, c.AppendSchema("mapping"),
						  "discriminator mapping has changed")))
				{
					yield return diff;
				}
			}
		}
	}
}