using System;
using System.Collections.Generic;

namespace Core.Common
{
    public abstract class AbstractRegistry<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _registry = new();

        protected void Register(TKey key, TValue entry) =>
            _registry.Add(key, entry);

        public TValue GetFor(TKey key)
        {
            if (_registry.TryGetValue(key, out TValue entry))
                return entry;

            throw new NotSupportedException($"No {nameof(TValue)} found for {key} key");
        }
    }
}