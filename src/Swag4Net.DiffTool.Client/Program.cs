using System;
using System.Collections.Generic;
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

    public enum Level
    {
        Info,
        Warning,
        Breaking
    }
    
    public abstract class DiffResult
    {
        public readonly Level Level;

        protected DiffResult(Level level)
        {
            Level = level;
        }
    }

    public class AddedResult : DiffResult
    {
        public readonly string? Value;

        public AddedResult(Level level, string? value) 
            : base(level)
        {
            Value = value;
        }
    }

    public class RemovedResult : DiffResult
    {
        public readonly string? Value;

        public RemovedResult(Level level, string? value) 
            : base(level)
        {
            Value = value;
        }
    }

    public class ModifiedResult : DiffResult
    {
        public readonly string? Original;

        public readonly string? Actual;

        public ModifiedResult(Level level, string? original, string? actual) 
            : base(level)
        {
            Original = original;
            Actual = actual;
        }
    }

    public class ApiComparer
    {
        public static IEnumerable<DiffResult> Compare(OpenApiDocument lhs, OpenApiDocument rhs)
        {
            var context = new ComparisonContext();
            foreach (DiffResult diffResult in Compare(context, lhs.Info, rhs.Info))
            {
                yield return diffResult;
            }
        }

        private static IEnumerable<DiffResult> Compare(ComparisonContext context, OpenApiInfo lhs, OpenApiInfo rhs)
        {
            
            throw new NotImplementedException();
        }
        
        internal class ComparisonContext
        {
            
        }
    }

}
