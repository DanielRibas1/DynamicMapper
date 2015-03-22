using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DynamicMapper.MapperMaker.Factories;
using DynamicMapper.TypeExtensions;

namespace DynamicMapper.MapperMaker
{
    public class CodeBuilder
    {
        public const string NAME = "name";
        public const string OUTPUT_NAMESPACE = "DynamicMapper.OnTheFly";
        public const string CLASSNAME_MASK = "{0}To{1}";
        public const string MAP = "Map";
        public const string REVERSE_MAP = "ReverseMap";

        private Type _inputType;
        private Type _outputType;

        #region Factories

        private CodeCompileUnitFactory CodeCompileUnitFactory;
        private CodeTypeFactory CodeTypeFactory;
        private CodeMapMethodFactory CodeMapMethodFactory;

        #endregion


        public CodeBuilder(Type inputType, Type outputType)
        {
            this._inputType = inputType;
            this._outputType = outputType;
            CodeCompileUnitFactory = new CodeCompileUnitFactory();
            CodeTypeFactory = new CodeTypeFactory();
            CodeMapMethodFactory = new CodeMapMethodFactory();
        }

        public CodeCompileUnit Make(Type mapperType)
        {
            var name = LinkNames(_inputType.Name, _outputType.Name);
            var createdCode =  CreateCode(mapperType, name);
            createdCode.UserData.Add(NAME, name);
            return createdCode;
        }

        private CodeCompileUnit CreateCode(Type MapperType, string name)
        {
            var codeUnit = CodeCompileUnitFactory.Get(OUTPUT_NAMESPACE, new[] { MapperType.Namespace });
            var codeType = CodeTypeFactory.Get(MapperType, name);
            ((CodeNamespace)codeUnit.UserData[OUTPUT_NAMESPACE]).Types.Add(codeType);
            codeType.Members.AddRange(CodeMapMethodFactory.Get(_inputType, _outputType, MAP));
            codeType.Members.AddRange(CodeMapMethodFactory.Get(_outputType, _inputType, REVERSE_MAP)); 
            return codeUnit;
        }

        private string LinkNames(string inputName, string outputName)
        {
            return String.Format(CLASSNAME_MASK, inputName, outputName);
        }     
    
    }
}
