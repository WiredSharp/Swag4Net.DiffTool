namespace Swag4Net.DiffTool.Client.Helpers
{
    internal static class NullableExtensions
    {
        public static string ToStringOrDefault<T>(this T? value, string defaultValue)
            where T:struct
        {
            return value?.ToString() ?? defaultValue;
        }
    }
}