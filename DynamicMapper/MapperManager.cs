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

        private static MapperManager _instance;
        public static MapperManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MapperManager();
                return _instance;
            }
        }

        private MapperManager() 
        {
            _store = new TypedMapperStore();
        }

        #endregion

        private TypedMapperStore _store;

        public ITypeMapper<TInput, TOutput> GetMapper<TInput, TOutput>()
        {
            Type mapperType;
            var key = MakeKey<TInput, TOutput>();
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
