using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CardDemo.Domain.Attributes;

namespace CardDemo.Data.Stores
{
    [Trace("Mmp.Runtime.Store", Ir = "store:IKeyedStore")]
    public interface IKeyedStore<TKey, TRecord> where TKey : notnull
    {
        ValueTask<TRecord?> GetAsync(TKey key, CancellationToken ct = default);
        ValueTask UpsertAsync(TKey key, TRecord record, CancellationToken ct = default);
        IAsyncEnumerable<TRecord> GetAllAsync(CancellationToken ct = default);
    }

    [Trace("Mmp.Runtime.Store", Ir = "store:MemoryKeyedStore")]
    public class MemoryKeyedStore<TKey, TRecord> : IKeyedStore<TKey, TRecord> where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, TRecord> _store = new();

        public ValueTask<TRecord?> GetAsync(TKey key, CancellationToken ct = default)
        {
            _store.TryGetValue(key, out var record);
            return ValueTask.FromResult(record);
        }

        public ValueTask UpsertAsync(TKey key, TRecord record, CancellationToken ct = default)
        {
            _store[key] = record;
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<TRecord> GetAllAsync([EnumeratorCancellation] CancellationToken ct = default)
        {
            foreach (var kvp in _store)
            {
                ct.ThrowIfCancellationRequested();
                yield return kvp.Value;
            }
            await Task.Yield();
        }
    }
}
