using System;

namespace NodeApi;

/// <summary>
/// Interface for a type that is convertible to and equatable to a JS value.
/// </summary>
/// <remarks>
/// This interface is implemented by:
///   - Value types that directly represent a specific type of JS value such as
///     <see cref="JSObject" />, <see cref="JSArray" />, etc.
///   - Collection proxy classes returned by <see cref="JSIterable.AsEnumerable()" /> and similar
///     methods, that forward all calls to a JS collection object.
///   - Interface proxy classes auto-generated by <see cref="JSExportAttribute" />, that forward
///     all calls to a JS object that implements the interface.
/// </remarks>
public interface IJSValue : IEquatable<JSValue>
{
    /// <summary>
    /// Gets the JS value for the current object.
    /// </summary>
    public JSValue Value { get; }
}
