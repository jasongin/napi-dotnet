using System;
using static NodeApi.JSNativeApi.Interop;

namespace NodeApi;

/// <summary>
/// A strong or weak reference to a JS value.
/// </summary>
/// <remarks>
/// <see cref="JSValue"/> and related JS handle structs are not valid after their
/// <see cref="JSValueScope"/> closes -- typically that is when a callback returns. Use a
/// <see cref="JSReference"/> to maintain a reference to a JS value beyond a single scope.
/// <para/>
/// A <see cref="JSReference"/> should be disposed when no longer needed; this allows the JS value
/// to be collected by the GC if it has no other references. The <see cref="JSReference"/> class
/// also has a finalizer so that the reference will be released when the C# object is GC'd. However
/// explicit disposal is still preferable when possible.
/// </remarks>
public class JSReference : IDisposable
{
    private readonly napi_env _env;
    private readonly napi_ref _handle;

    public bool IsWeak { get; private set; }

    public JSReference(JSValue value, bool isWeak = false)
    {
        _env = (napi_env)value.Scope;
        napi_create_reference(_env, (napi_value)value, isWeak ? 0u : 1u, out _handle).ThrowIfFailed();
        IsWeak = isWeak;
    }

    public JSReference(napi_ref handle, bool isWeak = false)
    {
        _env = (napi_env)JSSimpleValueScope.Current;
        _handle = handle;
        IsWeak = isWeak;
    }

    public void MakeWeak()
    {
        ThrowIfDisposed();
        if (!IsWeak)
        {
            napi_reference_unref(_env, _handle, nint.Zero).ThrowIfFailed();
            IsWeak = true;
        }
    }
    public void MakeStrong()
    {
        ThrowIfDisposed();
        if (IsWeak)
        {
            napi_reference_ref(_env, _handle, nint.Zero).ThrowIfFailed();
            IsWeak = true;
        }
    }

    public JSValue? GetValue()
    {
        ThrowIfDisposed();
        napi_get_reference_value(_env, _handle, out napi_value result).ThrowIfFailed();
        return result;
    }

    public static explicit operator napi_ref(JSReference value) => value._handle;

    public bool IsDisposed { get; private set; }

    private void ThrowIfDisposed()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(nameof(JSReference));
        }
    }

    /// <summary>
    /// Releases the reference.
    /// </summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!IsDisposed)
        {
            // The reference handle should be deleted regardless of the `disposing` argument.
            napi_status status = napi_delete_reference(_env, _handle);

            // Ignore errors when finalizing.
            if (disposing)
            {
                status.ThrowIfFailed();
            }

            IsDisposed = true;
        }
    }

    /// <summary>
    /// Deletes the reference to the JS value when it is no longer held by any C# code.
    /// </summary>
    ~JSReference() => Dispose(disposing: false);
}
