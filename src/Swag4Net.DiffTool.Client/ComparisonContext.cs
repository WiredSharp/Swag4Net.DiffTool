using System;
using Microsoft.OpenApi.Models;
using System.IO;

namespace Swag4Net.DiffTool.Client
{
    public sealed record ComparisonContext(string Path, OperationType? Method, string? Request, string? Response, string? Schema)
    {
        public static ComparisonContext FromPath(string path)
            => new ComparisonContext(path, null, null, null, null);

        public ComparisonContext AppendParameter(string segment)
        {
            if (segment == null) throw new ArgumentNullException(nameof(segment));
            return this with { Request = System.IO.Path.Combine(Request ?? "", segment) };
        }

        public ComparisonContext AppendResponse(string segment)
        {
            if (segment == null) throw new ArgumentNullException(nameof(segment));
            return this with { Response = System.IO.Path.Combine(Response ?? "", segment) };
        }

        public ComparisonContext AppendSchema(string segment)
        {
            if (segment == null) throw new ArgumentNullException(nameof(segment));
            return this with { Schema = System.IO.Path.Combine(Schema ?? "", segment) };
        }
    }
}