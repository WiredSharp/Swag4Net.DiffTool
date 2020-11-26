using System.Threading;

namespace Swag4Net.DiffTool.Client
{
   internal static class ID
   {
      private static long _nextId = 1;

      public static long Next => Interlocked.Increment(ref _nextId);
   }
}