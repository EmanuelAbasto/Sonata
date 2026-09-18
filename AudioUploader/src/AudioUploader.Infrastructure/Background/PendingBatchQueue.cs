using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AudioUploader.Infrastructure.Background
{
    public enum ItemReadiness
    {
        Ready,

        Deferred,

        Discard
    }

    public class PendingBatchQueue<TKey> where TKey : notnull
    {
        private readonly List<TKey> _items = new List<TKey>();
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly int _batchSize;

        public PendingBatchQueue(int batchSize)
        {
            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), "El tamaño de batch debe ser mayor a 0.");

            _batchSize = batchSize;
        }

        public async Task<bool> AddAsync(TKey item)
        {
            await _lock.WaitAsync();
            try
            {
                if (!_items.Contains(item))
                {
                    _items.Add(item);
                }
                return _items.Count >= _batchSize;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<List<TKey>> TakeReadyBatchAsync(Func<TKey, Task<ItemReadiness>> classify)
        {
            await _lock.WaitAsync();
            try
            {
                List<TKey> batch = new List<TKey>();
                List<TKey> deferred = new List<TKey>();
                int initialCount = _items.Count;

                for (int examined = 0; examined < initialCount && batch.Count < _batchSize && _items.Count > 0; examined++)
                {
                    TKey item = _items[0];
                    _items.RemoveAt(0);

                    ItemReadiness readiness = await classify(item);
                    switch (readiness)
                    {
                        case ItemReadiness.Ready:
                            batch.Add(item);
                            break;
                        case ItemReadiness.Deferred:
                            deferred.Add(item);
                            break;
                        case ItemReadiness.Discard:
                            break;
                    }
                }

                if (deferred.Count > 0)
                {
                    _items.AddRange(deferred);
                }

                return batch;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}