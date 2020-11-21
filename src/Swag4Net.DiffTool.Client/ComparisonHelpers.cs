using System;
using System.Collections.Generic;
using System.Linq;

namespace Swag4Net.DiffTool.Client
{
    internal static class ComparisonHelpers
    {
        public static IEnumerable<DiffResult> CompareTo<TValue>(
            this IDictionary<string, TValue> previous,
            IDictionary<string, TValue> actual,
            Func<string, ComparisonContext> getTarget,
            Func<TValue, TValue, ComparisonContext, IEnumerable<DiffResult>> compareValue)
            => CompareTo(previous, actual, String.Compare, compareValue, getTarget);

        public static IEnumerable<DiffResult> CompareTo<TKey, TValue>(
            this IDictionary<TKey, TValue> previous,
            IDictionary<TKey, TValue> actual,
            Func<TKey, TKey, int> compareKey,
            Func<TValue, TValue, ComparisonContext, IEnumerable<DiffResult>> compareValue,
            Func<TKey, ComparisonContext> getTarget) 
            where TKey:notnull
        {
            using var previousItems = previous.Keys.OrderBy(x => x).GetEnumerator();
            using var actualItems = actual.Keys.OrderBy(x => x).GetEnumerator();
            bool completed = false;
            while (!completed)
            {
                var previousContext = getTarget(previousItems.Current);
                var actualContext = getTarget(actualItems.Current);

                switch (previousItems.MoveNext(), actualItems.MoveNext())
                {
                    case (true, true):
                        if (compareKey(previousItems.Current, actualItems.Current) > 0)
                        {
                            yield return new DiffResult(DifferenceKind.Added, actualContext, $"'{actualItems.Current}' has been added");
                        }
                        else if (compareKey(previousItems.Current, actualItems.Current) < 0)
                        {
                            yield return new DiffResult(DifferenceKind.Removed, previousContext, $"'{previousItems.Current}' has been removed");
                        }
                        else
                        {
                            foreach (DiffResult diff in compareValue(previous[previousItems.Current], actual[actualItems.Current], previousContext))
                            {
                                yield return diff;
                            }
                        }
                        break;
                    case (false, false):
                        completed = true;
                        break;
                    case (true, false):
                        yield return new DiffResult(DifferenceKind.Removed, previousContext, $"'{previousItems.Current}' has been removed");
                        while (previousItems.MoveNext())
                        {
                            yield return new DiffResult(DifferenceKind.Removed, previousContext, $"'{previousItems.Current}' has been removed");
                        }
                        completed = true;
                        break;
                    case (false, true):
                        yield return new DiffResult(DifferenceKind.Added, actualContext, $"'{actualItems.Current}' has been added");
                        while (actualItems.MoveNext())
                        {
                            yield return new DiffResult(DifferenceKind.Added, actualContext, $"'{actualItems.Current}' has been added");
                        }
                        completed = true;
                        break;
                }
            }
        }
    }
}