using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamicMapper.Exceptions
{
    public class DynamicCompileException : Exception
    {
        private const string _errorMessage = "Errors detected after compiling the mapper assembly {0}, see ErrorCollection for more details";

        public CompilerErrorCollection ErrorCollection { get; set; }

        public DynamicCompileException(string assemblyName, CompilerErrorCollection errorCollection)
            : base(String.Format(_errorMessage, assemblyName))
        {
            this.ErrorCollection = errorCollection;
        }
    }
}
