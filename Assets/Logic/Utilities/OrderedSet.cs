using System.Collections.Generic;
using System.Linq;

namespace Logic.Utilities
{
    public class OrderedSet<T>
    {
        private List<T> _internal;

        public OrderedSet()
        {
            _internal = new List<T>();
        }

        public void Add(T item)
        {
            if (_internal.Contains(item)) _internal.Remove(item);
            _internal.Add(item);
        }

        public void Clear()
        {
            _internal.Clear();
        }

        public bool Contains(T item)
        {
            return _internal.Contains(item);
        }

        public bool Remove(T item)
        {
            return _internal.Remove(item);
        }

        public int Count
        {
            get { return _internal.Count; }
        }

        public T First()
        {
            return _internal.First();
        }
    }
}