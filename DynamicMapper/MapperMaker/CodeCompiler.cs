using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DynamicMapper.Exceptions;
using Microsoft.CSharp;

namespace DynamicMapper.MapperMaker
{
    public class CodeCompiler
    {
        private Type _inputType;
        private Type _outputType;

        public CodeCompiler(Type inputType, Type outputType)
        {
            this._inputType = inputType;
            this._outputType = outputType;
        }

        public Type GetGeneratedType(CodeCompileUnit codeUnit, string name)
        {
            var compileResult = this.CompileCode(codeUnit, name);
            var generatedType = this.GetGeneratedType(compileResult.CompiledAssembly, name);
            return generatedType;
        }

        private CompilerResults CompileCode(CodeCompileUnit codeUnit, string name)
        {
            var provider = new CSharpCodeProvider();
            var options = new CompilerParameters();

            options.GenerateExecutable = false;
            options.GenerateInMemory = true;
#if DEBUG
            options.IncludeDebugInformation = true;
            string debugSourceFile = name + "_DebugCodeFile.cs";
            var codeOptions = new CodeGeneratorOptions();
            using (var outputWriter = new IndentedTextWriter(new StreamWriter(debugSourceFile, false), "   "))
            {
                provider.GenerateCodeFromCompileUnit(codeUnit, outputWriter, codeOptions);
                outputWriter.Close();
            }
#else
            options.IncludeDebugInformation = false;
#endif
            options.OutputAssembly = name + "Mapper";
            options.ReferencedAssemblies.Add(this.GetType().Assembly.GetName().Name + ".dll");
            var inputAssemblyName = _inputType.Assembly.GetName().Name + ".dll";
            var outputAssemblyName = _outputType.Assembly.GetName().Name + ".dll";
            options.ReferencedAssemblies.Add(inputAssemblyName);
            if (!inputAssemblyName.Equals(outputAssemblyName))
                options.ReferencedAssemblies.Add(outputAssemblyName);

            var compileResult = provider.CompileAssemblyFromDom(options, codeUnit);
            if (compileResult.Errors.HasErrors)
                throw new DynamicCompileException(options.OutputAssembly, compileResult.Errors);
            return compileResult;
        }

        private Type GetGeneratedType(Assembly generatedAssembly, string name)
        {
            Type targetType = null;
            foreach (var type in generatedAssembly.GetTypes())
            {
                if (type.Name.Contains(name))
                    targetType = type;
            }
            return targetType;
        }
    }
}
