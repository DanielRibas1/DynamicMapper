using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom;
using DynamicMapper.Interfaces;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using System.IO;
using System.Diagnostics;


namespace DynamicMapper.MapperMaker
{
    public class MapperTypeGenerator<TInput, TOutput>
    {        
        public Type Make()
        {
            var codeGenerator = new CodeBuilder(typeof(TInput), typeof(TOutput));
            var codeUnit = codeGenerator.Make(typeof(ITypeMapper<TInput, TOutput>));
            var name = codeUnit.UserData[CodeBuilder.NAME] as string;
            var codeCompiler = new CodeCompiler(typeof(TInput), typeof(TOutput));
            return codeCompiler.GetGeneratedType(codeUnit, name);         
        }       
    }
}
