using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamicMapper.Exceptions
{
    public class MapperInstanciateException : Exception
    {
        private const string _errorMessage = "Unable to instanciate mapper of type {0}, see inner exception for more details";

        public Type MapperType { get; set; }

        public MapperInstanciateException(Type mapperType, Exception innerEx)
            : base(String.Format(_errorMessage, mapperType.Name), innerEx)
        {
            this.MapperType = mapperType;
        }
    }
}
