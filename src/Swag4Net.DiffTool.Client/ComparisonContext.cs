using Microsoft.OpenApi.Models;

namespace Swag4Net.DiffTool.Client
{
    public sealed record ComparisonContext(string Path, OperationType? Method, string? Parameter, string? Response,
        string? FieldPath)
    {
        public static ComparisonContext FromPath(string path)
            => new ComparisonContext(path, null, null, null, null);
    }
}