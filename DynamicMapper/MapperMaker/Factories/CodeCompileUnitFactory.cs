using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamicMapper.MapperMaker.Factories
{
    class CodeCompileUnitFactory
    {
        public CodeCompileUnit Get(string namespaceName, string[] imports)
        {            
            //Code 'File'
            var codeUnit = new CodeCompileUnit();
            //Namespace
            var codeNamespace = new CodeNamespace(namespaceName);
            codeUnit.Namespaces.Add(codeNamespace);
            codeUnit.UserData.Add(namespaceName, codeNamespace);
            //using imports
            foreach (var import in imports)
                codeNamespace.Imports.Add(new CodeNamespaceImport(import));    
            return codeUnit;
        }
    }
}
