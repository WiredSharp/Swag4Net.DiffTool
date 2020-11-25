using System;
using System.Collections.Generic;
using System.Linq;

namespace Swag4Net.DiffTool.Client
{
    internal static class ComparisonHelpers
    {
        public static IEnumerable<DiffResult> NopCompare<T>(T previous, T actual, ComparisonContext context)
        {
            return Array.Empty<DiffResult>();
        }

        public static IEnumerable<DiffResult> CompareTo<TValue>(this IEnumerable<TValue> previous,
            IEnumerable<TValue> actual,
            Func<TValue, ComparisonContext> getContext, Func<TValue, TValue, int> compare)
            where TValue: notnull
        {
            previous ??= Array.Empty<TValue>();
            actual ??= Array.Empty<TValue>();
            return previous.ToDictionary(x => x).CompareTo(actual.ToDictionary(x => x), compare, NopCompare, getContext);
        }

        public static IEnumerable<DiffResult> CompareTo<TKey, TValue>(this IEnumerable<TValue> previous,
            IEnumerable<TValue> actual,
            Func<TValue, TKey> getKey,
            Func<TKey, ComparisonContext> getContext, Func<TKey, TKey, int> compareKey)
            where TKey: notnull
        {
            previous ??= Array.Empty<TValue>();
            actual ??= Array.Empty<TValue>();
            return previous.ToDictionary(getKey).CompareTo(actual.ToDictionary(getKey), compareKey, NopCompare, getContext);
        }

        public static IEnumerable<DiffResult> CompareTo<TValue>(
            this IDictionary<string, TValue> previous,
            IDictionary<string, TValue> actual,
            Func<string, ComparisonContext> getContext,
            Func<TValue, TValue, ComparisonContext, IEnumerable<DiffResult>> compareValue)
            => CompareTo(previous, actual, String.Compare, compareValue, getContext);

        public static IEnumerable<DiffResult> CompareTo<TKey, TValue>(
            this IDictionary<TKey, TValue>? previous,
            IDictionary<TKey, TValue>? actual,
            Func<TKey, TKey, int> compareKey,
            Func<TValue, TValue, ComparisonContext, IEnumerable<DiffResult>> compareValue,
            Func<TKey, ComparisonContext> getContext) 
            where TKey:notnull
        {
            previous ??= new Dictionary<TKey, TValue>();
            actual ??= new Dictionary<TKey, TValue>();
            using var previousKeys = previous.Keys.OrderBy(x => x).GetEnumerator();
            using var actualKeys = actual.Keys.OrderBy(x => x).GetEnumerator();
            bool hasPrevious = previousKeys.MoveNext();
            bool hasActual = actualKeys.MoveNext();
            bool completed = false;
            while (!completed)
            {
                ComparisonContext? previousContext;
                ComparisonContext? actualContext;
                switch (hasPrevious, hasActual)
                {
                    case (true, true):
                        previousContext = getContext(previousKeys.Current);
                        actualContext = getContext(actualKeys.Current);
                        if (compareKey(previousKeys.Current, actualKeys.Current) > 0)
                        {
                            yield return new DiffResult(DifferenceKind.Added, actualContext, $"'{actualKeys.Current}' has been added");
                            hasActual = actualKeys.MoveNext();
                        }
                        else if (compareKey(previousKeys.Current, actualKeys.Current) < 0)
                        {
                            yield return new DiffResult(DifferenceKind.Removed, previousContext, $"'{previousKeys.Current}' has been removed");
                            hasPrevious = previousKeys.MoveNext();
                        }
                        else
                        {
                            foreach (DiffResult diff in compareValue(previous[previousKeys.Current], actual[actualKeys.Current], previousContext))
                            {
                                yield return diff;
                            }
                            hasActual = actualKeys.MoveNext();
                            hasPrevious = previousKeys.MoveNext();
                        }
                        break;
                    case (false, false):
                        completed = true;
                        break;
                    case (true, false):
                        previousContext = getContext(previousKeys.Current);
                        yield return new DiffResult(DifferenceKind.Removed, previousContext, $"'{previousKeys.Current}' has been removed");
                        while (previousKeys.MoveNext())
                        {
                            yield return new DiffResult(DifferenceKind.Removed, previousContext, $"'{previousKeys.Current}' has been removed");
                        }
                        completed = true;
                        break;
                    case (false, true):
                        actualContext = getContext(actualKeys.Current);
                        yield return new DiffResult(DifferenceKind.Added, actualContext, $"'{actualKeys.Current}' has been added");
                        while (actualKeys.MoveNext())
                        {
                            yield return new DiffResult(DifferenceKind.Added, actualContext, $"'{actualKeys.Current}' has been added");
                        }
                        completed = true;
                        break;
                }
            }
        }

        public static DiffResult? CompareBool(bool previous, bool actual, ComparisonContext context, string switchOffMessage, string switchOnMessage)
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

        public static DiffResult? CompareBool(bool? previous, bool? actual, ComparisonContext context, string switchOffMessage, string switchOnMessage)
        {
            return Enumerable.FirstOrDefault<DiffResult>(HandleNullable(previous, actual, compareExisting, context));

            IEnumerable<DiffResult> compareExisting(bool previousValue, bool actualValue, ComparisonContext ctx)
            {
                yield return CompareBool(previousValue, actualValue, ctx, switchOffMessage, switchOnMessage)!;
            }
        }

        public static IEnumerable<DiffResult> CompareScalar<T>(T? previous, T? actual, ComparisonContext context, string changedMessage)
            where T:struct
        {
            return HandleNullable(previous, actual, compareExisting, context);

            IEnumerable<DiffResult> compareExisting(T previousValue, T actualValue, ComparisonContext ctx)
            {
                if (!previousValue!.Equals(actualValue))
                {
                    yield return new DiffResult(DifferenceKind.Modified, context, $"{changedMessage} from {previousValue} to {actualValue}");
                }
            }
        }

        public static IEnumerable<DiffResult> CompareScalar<T>(T? previous, T? actual, ComparisonContext context, string changedMessage)
            where T : class
        {
            return HandleNullValue(previous, actual, compareExisting, context);

            IEnumerable<DiffResult> compareExisting(T previousValue, T actualValue, ComparisonContext ctx)
            {
                if (!previousValue!.Equals(actualValue))
                {
                    yield return new DiffResult(DifferenceKind.Modified, context, $"{changedMessage} from '{previousValue}' to '{actualValue}'");
                }
            }
        }

        private static IEnumerable<DiffResult> HandleNullable<T>(T? previous, T? actual
            , Func<T, T, ComparisonContext, IEnumerable<DiffResult>> compareExisting, ComparisonContext context)
            where T:struct
        {
            switch (previous.HasValue, actual.HasValue)
            {
                case (false, true):
                    yield return new DiffResult(DifferenceKind.Added, context);
                    break;
                case (true, false):
                    yield return new DiffResult(DifferenceKind.Removed, context);
                    break;
                case (true, true):
                    foreach (DiffResult diffResult in compareExisting(previous!.Value, actual!.Value, context))
                    {
                        yield return diffResult;
                    }
                    break;
            }
        }

        public static IEnumerable<DiffResult> HandleNullValue<T>(T? previous, T? actual
            , Func<T, T, ComparisonContext, IEnumerable<DiffResult>> compareExisting, ComparisonContext context)
            where T : class
        {
            switch (previous != null, actual != null)
            {
                case (false, true):
                    yield return new DiffResult(DifferenceKind.Added, context);
                    break;
                case (true, false):
                    yield return new DiffResult(DifferenceKind.Removed, context);
                    break;
                case (true, true):
                    foreach (DiffResult diffResult in compareExisting(previous!, actual!, context))
                    {
                        yield return diffResult;
                    }
                    break;
            }
        }
    }
}