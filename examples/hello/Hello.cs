
using System;

namespace NodeApi.Examples;

public static class Hello
{
    /// <summary>
    /// Gets a greeting string for testing.
    /// </summary>
    /// <param name="greeter">Name of the greeter.</param>
    /// <returns>A greeting with the name.</returns>
    [JSExport("hello")]
    public static string Test(string greeter)
    {
        Console.WriteLine($"Hello {greeter}!");
        return $"Hello {greeter}!";
    }
}
