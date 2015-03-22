using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DynamicMapper.Interfaces;

namespace DynamicMapper.MapperStorage
{
    public class TypedMapperStore
    {
        Dictionary<MapperKey, Type> _innerStore = new Dictionary<MapperKey, Type>();

        public void Put(MapperKey key, Type mapper)
        {
            if (_innerStore.ContainsKey(key))
                _innerStore[key] = mapper;
            else
                _innerStore.Add(key, mapper);
        }

        public void Remove(MapperKey key)
        {
            _innerStore.Remove(key);
        }

        public bool Contains(MapperKey key)
        {
            return _innerStore.ContainsKey(key);
        }

        public Type Get<TInput, TOutput>(MapperKey key)
        {
            if (_innerStore.ContainsKey(key))
            {
                return _innerStore[key];   
            }
            else
            {
                throw new KeyNotFoundException("Mapper not found");
            }
        }

        
    }
}
