using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DynamicMapper.Exceptions;
using DynamicMapper.Interfaces;
using DynamicMapper.MapperMaker;
using DynamicMapper.MapperStorage;

namespace DynamicMapper
{
    public sealed class MapperManager
    {
        #region Singleton Implementation

        private static object _creationLock = new object();
        private static MapperManager _instance;
        public static MapperManager Instance
        {
            get
            {
                if (_instance == null)
                    lock(_creationLock)      
                    {
                        if (_instance == null)                        
                            _instance = new MapperManager();   
                    }
                return _instance;
            }
        }

        private MapperManager() 
        {
            _store = new TypedMapperStore();
            _locks = new Dictionary<MapperKey, object>();
        }

        #endregion

        private TypedMapperStore _store;        
        private Dictionary<MapperKey, object> _locks;
        private object _lockMakerlock = new object();

        public ITypeMapper<TInput, TOutput> GetMapper<TInput, TOutput>()
        {
            Type mapperType = null;var key = MakeKey<TInput, TOutput>();
            var xlock = GetLock(key);
            lock (xlock)
            {
                if (_store.Contains(key))
                {                
                    mapperType = _store.Get<TInput, TOutput>(key);                
                }            
                else
                {               
                    var generator = new MapperTypeGenerator<TInput, TOutput>();
                    mapperType = generator.Make();
                    _store.Put(key, mapperType);                   
                }
            }
            if (mapperType == null)
                throw new Exception();
            return this.InstanciateType<TInput, TOutput>(mapperType) as ITypeMapper<TInput, TOutput>;
        }

        private object InstanciateType<TInput, TOutput>(Type mapper)
        {
            if (mapper == null)
                throw new ArgumentNullException("mapper");
            try
            {
                var instance = Activator.CreateInstance(mapper.MakeGenericType(typeof(TInput), typeof(TOutput)));
                return instance;
            }
            catch (Exception ex)
            {
                throw new MapperInstanciateException(mapper, ex);
            }
        }

        private object GetLock(MapperKey key)
        {
            lock (_lockMakerlock)
            {
                if (!_locks.ContainsKey(key))
                {
                    var xlock = new object();
                    _locks.Add(key, xlock);
                    return xlock;
                }
                return _locks[key];
            }
        }

        #region Key Maker

        public static MapperKey MakeKey(Type input, Type output)
        {
            return new MapperKey(input, output);
        }

        public static MapperKey MakeKey<TInput, TOutput>()
        {
            return new MapperKey(typeof(TInput), typeof(TOutput));
        }

        #endregion

    }
}
