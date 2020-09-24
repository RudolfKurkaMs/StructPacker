using System;

namespace RudolfKurka.StructPacker
{
    public struct DisposableCallback : IDisposable
    {
        private readonly Action _callback;
        private bool _disposed;

        public DisposableCallback(Action callback)
        {
            _callback = callback;
            _disposed = false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _callback?.Invoke();
        }
    }
}