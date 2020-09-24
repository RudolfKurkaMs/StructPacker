using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace StructPacker
{
    public class PooledBuffer : IDisposable
    {
        private static readonly LinkedList<PooledBuffer> SlowCache = new LinkedList<PooledBuffer>();

        private static readonly Stack<PooledBuffer>
            TwoByteCache = new Stack<PooledBuffer>(),
            FourByteCache = new Stack<PooledBuffer>(),
            SixteenByteCache = new Stack<PooledBuffer>();

        private int _isDisposed;

        public byte[] Data { get; }
        
        public int Size { get; private set; }

        private PooledBuffer(int dataSize)
        {
            Data = new byte[dataSize];
            Size = dataSize;
        }

        public void Dispose()
        {
            if (Interlocked.Increment(ref _isDisposed) == 1)
                ReclaimSlice(this);
        }

        public void WriteInto(Stream stream) => stream.Write(Data, 0, Size);

        public Task WriteIntoAsync(Stream stream) => stream.WriteAsync(Data, 0, Size);

        public Task WriteIntoAsync(Stream stream, CancellationToken ct) => stream.WriteAsync(Data, 0, Size, ct);

        public byte[] ToArray()
        {
            var data = new byte[Size];
            Buffer.BlockCopy(Data, 0, data, 0, Size);
            return data;
        }

        private void Reset(int count)
        {
            Size = count;
            _isDisposed = 0;
        }

        public static void ClearCache()
        {
            lock (TwoByteCache)
                TwoByteCache.Clear();

            lock (FourByteCache)
                FourByteCache.Clear();

            lock (SixteenByteCache)
                SixteenByteCache.Clear();

            lock (SlowCache)
                SlowCache.Clear();
        }

        public static PooledBuffer Get(int size)
        {
            switch (size)
            {
                case 16: //most likely
                    lock (SixteenByteCache)
                        return SixteenByteCache.Count > 0 ? SixteenByteCache.Pop() : new PooledBuffer(size);
                case 2:
                    lock (TwoByteCache)
                        return TwoByteCache.Count > 0 ? TwoByteCache.Pop() : new PooledBuffer(size);
                case 4:
                    lock (FourByteCache)
                        return FourByteCache.Count > 0 ? FourByteCache.Pop() : new PooledBuffer(size);
                case 0:
                    throw new ArgumentOutOfRangeException(nameof(size));
                default:
                    lock (SlowCache)
                    {
                        if (SlowCache.Count > 0)
                        {
                            LinkedListNode<PooledBuffer> node = SlowCache.First;

                            if (node.Value.Data.Length >= size)
                            {
                                SlowCache.Remove(node);
                                node.Value.Reset(size);

                                return node.Value;
                            }
                        }

                        return new PooledBuffer(size);
                    }
            }
        }

        private static void ReclaimSlice(PooledBuffer slice)
        {
            switch (slice.Data.Length)
            {
                case 16: //most likely
                    lock (SixteenByteCache)
                        SixteenByteCache.Push(slice);
                    return;
                case 2:
                    lock (TwoByteCache)
                        TwoByteCache.Push(slice);
                    return;
                case 4:
                    lock (FourByteCache)
                        FourByteCache.Push(slice);
                    return;
                default:
                    lock (SlowCache)
                    {
                        if (SlowCache.Count > 0)
                        {
                            LinkedListNode<PooledBuffer> node = SlowCache.First;
                            int size = slice.Data.Length;

                            while (node.Next != null)
                            {
                                if (node.Value.Data.Length > size)
                                {
                                    node = node.Next;
                                    continue;
                                }

                                break;
                            }

                            if (node.Value.Data.Length >= size)
                                SlowCache.AddAfter(node, slice);
                            else
                                SlowCache.AddBefore(node, slice);
                        }
                        else
                        {
                            SlowCache.AddLast(slice);
                        }
                    }

                    return;
            }
        }
    }
}