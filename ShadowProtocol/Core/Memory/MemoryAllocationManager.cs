using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace ShadowProtocol.Core.Memory
{
    public unsafe class ArenaAllocator : IDisposable
    {
        private readonly byte* _buffer;
        private readonly long _totalSizeBytes;
        private long _currentOffset;
        private bool _isDisposed;

        public long AllocatedBytes => _currentOffset;
        public long CapacityBytes => _totalSizeBytes;

        public ArenaAllocator(long sizeBytes)
        {
            _totalSizeBytes = sizeBytes;
            _buffer = (byte*)Marshal.AllocHGlobal(new IntPtr(sizeBytes));
            _currentOffset = 0;
            _isDisposed = false;
        }

        public void* Allocate(long bytes, int alignment = 8)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(ArenaAllocator));

            long currentPtr = (long)(_buffer + _currentOffset);
            long alignedPtr = (currentPtr + (alignment - 1)) & ~(alignment - 1);
            long newOffset = (alignedPtr - (long)_buffer) + bytes;

            if (newOffset > _totalSizeBytes)
            {
                throw new OutOfMemoryException($"ArenaAllocator out of memory: requested {bytes} bytes, capacity {_totalSizeBytes}");
            }

            _currentOffset = newOffset;
            return (void*)alignedPtr;
        }

        public void Reset()
        {
            _currentOffset = 0;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Marshal.FreeHGlobal(new IntPtr(_buffer));
                _isDisposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~ArenaAllocator()
        {
            Dispose();
        }
    }

    public class MemoryDiagnostics
    {
        public long TotalNativeAllocatedBytes { get; set; }
        public long TotalManagedAllocatedBytes { get; set; }
        public int ActivePoolCount { get; set; }
        public int GarbageCollectionGen0Count { get; set; }
        public int GarbageCollectionGen1Count { get; set; }
        public int GarbageCollectionGen2Count { get; set; }

        public static MemoryDiagnostics Capture()
        {
            return new MemoryDiagnostics
            {
                TotalManagedAllocatedBytes = GC.GetTotalMemory(false),
                GarbageCollectionGen0Count = GC.CollectionCount(0),
                GarbageCollectionGen1Count = GC.CollectionCount(1),
                GarbageCollectionGen2Count = GC.CollectionCount(2)
            };
        }
    }
}
