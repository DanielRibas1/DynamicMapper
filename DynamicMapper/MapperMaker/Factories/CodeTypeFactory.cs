using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DynamicMapper.Interfaces;

namespace DynamicMapper.MapperMaker.Factories
{
    class CodeTypeFactory
    {
        public CodeTypeDeclaration Get(Type mapperInterface, string className)
        {
            var codeClass = new CodeTypeDeclaration(className);
            foreach (var typedParam in mapperInterface.GetGenericTypeDefinition().GetGenericArguments().Select(x => x.Name))
                codeClass.TypeParameters.Add(new CodeTypeParameter(typedParam));
            codeClass.BaseTypes.Add(new CodeTypeReference(mapperInterface));
            return codeClass;
        }
    }
}
