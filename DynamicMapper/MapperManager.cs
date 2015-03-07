using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
            _store = new MapperStore();
        }

        #endregion

        private MapperStore _store;

        public ITypeMapper<TInput, TOutput> GetMapper<TInput, TOutput>()
        {
            var key = MakeKey<TInput, TOutput>();
            if (_store.Contains(key))
            {
                return _store.Get<TInput, TOutput>(key);
            }
            else
            {
                var generator = new MapperGen<TInput, TOutput>();
                var generatedInstance = generator.Make();
                _store.Put(key, generatedInstance);
                return generatedInstance;
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
