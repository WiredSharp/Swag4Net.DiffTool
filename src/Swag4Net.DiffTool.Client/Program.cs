using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;

namespace Swag4Net.DiffTool.Client
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");
		}
	}
	
	public class ComparisonContext
	{
		public string Path { get; }
		
		public string Operation { get; set; }
		
		public OperationType? Method { get; set; }

		public string Parameter { get; set; }

		public string Response { get; set; }

		public ComparisonContext(string path)
		{
			if (path is null) throw new ArgumentNullException(nameof(path));
			Path = path;
		}
	}

	public enum DifferenceKind
	{
		Added,
		Removed,
		Modified
	}

	public class DiffResult
	{
		public ComparisonContext Context { get; set; }

		public DifferenceKind Kind { get; set; }

		public string? Message { get; set; }

		public DiffResult(DifferenceKind kind, ComparisonContext context)
		{
			if (context is null) throw new ArgumentNullException(nameof(context));
			Kind = kind;
			Context = context;
		}
	}

	public class ApiComparer
	{
		public static IEnumerable<DiffResult> Compare(OpenApiDocument previous, OpenApiDocument actual)
		{
			foreach (DiffResult diffResult in Compare(previous.Paths, actual.Paths))
			{
				yield return diffResult;
			}
		}

		private static IEnumerable<DiffResult> Compare(OpenApiPaths previous, OpenApiPaths actual) 
			=> previous.Compare(actual, path => , Compare);

		private static IEnumerable<DiffResult> Compare(OpenApiPathItem previous, OpenApiPathItem actual)
		{
			if (previous.Parameters.Any() && actual.Parameters.Any())
				// shared parameters should be added to each operation parameters comparison
				throw new NotSupportedException("route shared parameters is not supported");
			return previous.Operations.Compare(actual.Operations, ComparisonTarget.Operation
				, (k1, k2) => (int)k1 - (int)k2, Compare, method => method.ToString());
		}

		private static IEnumerable<DiffResult> Compare(OpenApiOperation previous, OpenApiOperation actual)
		{
			foreach (var diff in previous.Parameters
				.ToDictionary(p => p.Name)
				.Compare(actual.Parameters.ToDictionary(p => p.Name)
					, ComparisonTarget.Request
					, Compare))
			{
				yield return diff;
			}
			
			foreach (var diff in Compare(previous.RequestBody, actual.RequestBody))
			{
				yield return diff;
			}

			switch (previous.Deprecated, actual.Deprecated)
			{
				case (true, false):
					yield return new DiffResult(DifferenceKind.Modified);
			}
		}

		private static IEnumerable<DiffResult> Compare(OpenApiRequestBody previous, OpenApiRequestBody actual)
		{
			throw new NotImplementedException();
		}

		private static IEnumerable<DiffResult> Compare(OpenApiParameter previous, OpenApiParameter actual)
		{
			throw new NotImplementedException();
		}
		private static IEnumerable<DiffResult> Compare(OpenApiInfo previous, OpenApiInfo actual)
		{
			throw new NotImplementedException();
		}

		private class ComparisonContext
		{

		}
	}

	internal static class ComparisonHelpers
	{
		public static IEnumerable<DiffResult> Compare<TValue>(
			this IDictionary<string, TValue> previous,
			IDictionary<string, TValue> actual,
			Func<TValue, ComparisonContext> getTarget,
			Func<TValue, TValue, IEnumerable<DiffResult>> compareValue)
		=> Compare<string, TValue>(previous, actual, String.Compare, compareValue, getTarget);

		public static IEnumerable<DiffResult> Compare<TKey, TValue>(
			this IDictionary<TKey, TValue> previous,
			IDictionary<TKey, TValue> actual,
			Func<TKey, TKey, int> compareKey,
			Func<TValue, TValue, IEnumerable<DiffResult>> compareValue,
			Func<TKey, ComparisonContext> getTarget) 
			where TKey:notnull
		{
			using var previousPaths = previous.Keys.OrderBy(x => x).GetEnumerator();
			using var actualPaths = actual.Keys.OrderBy(x => x).GetEnumerator();
			bool completed = false;
			while (!completed)
			{
				switch (previousPaths.MoveNext(), actualPaths.MoveNext())
				{
					case (true, true):
						if (compareKey(previousPaths.Current, actualPaths.Current) > 0)
						{
							yield return new DiffResult(DifferenceKind.Added, getTarget(actualPaths.Current));
						}
						else if (compareKey(previousPaths.Current, actualPaths.Current) < 0)
						{
							yield return new DiffResult(DifferenceKind.Removed, getTarget(previousPaths.Current));
						}
						else
						{
							foreach (DiffResult diff in compareValue(previous[previousPaths.Current], actual[actualPaths.Current]))
							{
								yield return diff;
							}
						}
						break;
					case (false, false):
						completed = true;
						break;
					case (true, false):
						yield return new DiffResult(DifferenceKind.Removed, getTarget(previousPaths.Current));
						while (previousPaths.MoveNext())
						{
							yield return new DiffResult(DifferenceKind.Removed, getTarget(previousPaths.Current));
						}
						completed = true;
						break;
					case (false, true):
						yield return new DiffResult(DifferenceKind.Added, getTarget(actualPaths.Current));
						while (actualPaths.MoveNext())
						{
							yield return new DiffResult(DifferenceKind.Added, getTarget(actualPaths.Current));
						}
						completed = true;
						break;
				}
			}
		}
	}
}
