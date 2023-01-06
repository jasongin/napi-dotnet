using System;
using System.Threading;

namespace NodeApi.Examples;

public class Counter
{
    // TODO: Support exporting static classes without a constructor.
    public Counter(JSCallbackArgs _)
    {
        Console.WriteLine("Counter()");
    }

    private static uint count;

    public static JSValue Count(JSCallbackArgs _)
    {
        var result = Interlocked.Increment(ref count);

        Console.WriteLine($"Counter.Count() => {result}");

        return result;
    }
}
