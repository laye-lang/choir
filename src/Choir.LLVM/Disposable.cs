namespace Choir.LibLLVM;

public abstract class Disposable
    : IDisposable
{
    private bool _disposed;

    protected virtual void DisposeManaged() { }
    protected virtual void DisposeUnmanaged() { }

    private void Dispose(bool managed)
    {
        if (_disposed) return;

        if (managed) DisposeManaged();
        DisposeUnmanaged();

        _disposed = true;
    }

    ~Disposable()
    {
        Dispose(managed: false);
    }

    public void Dispose()
    {
        Dispose(managed: true);
        GC.SuppressFinalize(this);
    }
}
