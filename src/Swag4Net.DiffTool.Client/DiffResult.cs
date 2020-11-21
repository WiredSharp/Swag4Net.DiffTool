using System;

namespace Swag4Net.DiffTool.Client
{
    public class DiffResult
    {
        public ComparisonContext Context { get; set; }

        public DifferenceKind Kind { get; }

        public string? Message { get; set; }

        public DiffResult(DifferenceKind kind, ComparisonContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            Kind = kind;
            Context = context;
        }
		
        public DiffResult(DifferenceKind kind, ComparisonContext context, string message)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            Kind = kind;
            Context = context;
            Message = message;
        }
    }
    
    public enum DifferenceKind
    {
        Added,
        Removed,
        Modified
    }
}