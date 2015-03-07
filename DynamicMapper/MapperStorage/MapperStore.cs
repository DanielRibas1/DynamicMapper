using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DynamicMapper.Interfaces;

namespace DynamicMapper.MapperStorage
{
    public class MapperStore
    {
        Dictionary<MapperKey, IMapper> _innerStore = new Dictionary<MapperKey, IMapper>();

        public void Put(MapperKey key, IMapper mapper)
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

        public ITypeMapper<TInput, TOutput> Get<TInput, TOutput>(MapperKey key)
        {
            if (_innerStore.ContainsKey(key))
            {
                IMapper simpleMapper = _innerStore[key];
                try
                {
                    var typedMapper = simpleMapper as ITypeMapper<TInput, TOutput>;
                    return typedMapper;
                }
                catch (Exception ex)
                {
                    throw;
                }
                
            }
            else
            {
                throw new KeyNotFoundException("Mapper not found");
            }
        }

        
    }
}
