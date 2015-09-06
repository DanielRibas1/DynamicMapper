using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamicMapper.Exceptions
{
    public class MissmatchArrayAssignException : Exception
    {
        private const string _errorMessage = "Missmatch on assignment with array types with {0} and {1} on property {2} inside type {3}";

        public MissmatchArrayAssignException(Type inputType, Type outputType, string propertyName, Type parentType)
            : base(String.Format(_errorMessage, inputType.Name, outputType.Name, propertyName, parentType.Name))
        {
        }
    }
}
