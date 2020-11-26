using System;
using System.Collections.Generic;
using Microsoft.OpenApi.Models;
using System.IO;

namespace Swag4Net.DiffTool.Client
{
    public sealed record ComparisonContext(string Path, OperationType? Method, string? Request, string? Response, string? Schema)
    {
        private Stack<string> SchemaStack { get; } = new Stack<string>();
        
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

        public ComparisonContext AppendType(string segment)
        {
            if (segment == null) throw new ArgumentNullException(nameof(segment));
            return this with { Schema = Schema == null ? $".{segment}" : $"{Schema}.{segment}" };
        }

        public ComparisonContext AppendAttribute(string segment)
        {
            if (segment == null) throw new ArgumentNullException(nameof(segment));
            return this with { Schema = Schema == null ? $"@{segment}" : $"{Schema}@{segment}" };
        }

        public bool IsSchemaStacked(string schemaId) => SchemaStack.Contains(schemaId);

        public bool PushSchema(string schemaId)
        {
            if (!IsSchemaStacked(schemaId))
            {
                SchemaStack.Push(schemaId);
                return true;
            }
            else
            {
                return false;
            }
        }

        public string PopSchema() => SchemaStack.Pop();
    }
}