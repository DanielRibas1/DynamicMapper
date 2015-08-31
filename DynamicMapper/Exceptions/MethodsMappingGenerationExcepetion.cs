using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamicMapper.Exceptions
{
    public class MethodsMappingGenerationExcepetion : Exception
    {
        private const string _errorMessage = "Error generating dynamic method to map {0} property from {1} to {2}. See inner exception for more details";

        public Type InputType { get; set; }
        public Type OutputType { get; set; }
        public string PropertyName { get; set; }

        public MethodsMappingGenerationExcepetion(Type input, Type output, string name, Exception innerEx)
            : base(String.Format(_errorMessage, name, input.Name, output.Name), innerEx)
        {
            this.InputType = input;
            this.OutputType = output;
            this.PropertyName = name;
        }
    }
}
