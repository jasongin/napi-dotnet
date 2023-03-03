using System;

namespace NodeApi.TestCases;

public static class Hello
{
    /// <summary>
    /// Gets a greeting string - an example of Node.js calling .NET.
    /// </summary>
    [JSExport("hello")]
    public static string Test(string greeter)
    {
        Console.WriteLine($"Hello {greeter}!");
        return $"Hello {greeter}!";
    }
}
