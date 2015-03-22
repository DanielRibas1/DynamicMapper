using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DynamicMapper.TypeExtensions;

namespace DynamicMapper.MapperMaker.Factories
{
    class CodeMapMethodFactory
    {
        private const string ENTRY = "entry";
        private const string OUTPUT = "output";

        private List<CodeMemberMethod> _recursiveMethods = new List<CodeMemberMethod>();

        public CodeMemberMethod[] Get(Type input, Type output, string name)
        {
            _recursiveMethods.Clear();
            var method = MakeMethod(name, input, output);
            method.Name = name;
            if (_recursiveMethods.Count > 0)
                return (new[] { method }).Concat(_recursiveMethods).ToArray();            
            return new[] { method };
        }

        private CodeMemberMethod MakeBody(string name, CodeParameterDeclarationExpression entryParamExpression, Type returnType)
        {
            var method = new CodeMemberMethod();
            method.Attributes = MemberAttributes.Public;
            method.Parameters.Add(entryParamExpression);
            var entryParamReference = new CodeVariableReferenceExpression(ENTRY);
            method.ReturnType = new CodeTypeReference(returnType);
            return method;
        }

        private CodeVariableReferenceExpression SetNewDeclaration(CodeMemberMethod method, Type varType, string varName)
        {
            method.Statements.Add(new CodeVariableDeclarationStatement(varType, varName));
            var reference = new CodeVariableReferenceExpression(varName);
            method.UserData[varName] = reference;
            return reference;
        }

        private void SetActivatorExpresion(CodeMemberMethod method, CodeVariableReferenceExpression assignVar)
        {
            var activatorReference = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(typeof(System.Activator)), "CreateInstance");
            activatorReference.TypeArguments.Add(method.ReturnType);
            method.Statements.Add(
                new CodeAssignStatement(
                    assignVar, 
                    new CodeMethodInvokeExpression(activatorReference)));
        }

        private CodeMemberMethod MakeMethod(string name, Type input, Type output)
        {
            var entryParamDeclaration = new CodeParameterDeclarationExpression(new CodeTypeReference(input), ENTRY);
            var method = this.MakeBody(name, entryParamDeclaration, output);
            var entryParam = new CodeVariableReferenceExpression(ENTRY);
            var outputVar = this.SetNewDeclaration(method, output, OUTPUT);             
            this.SetActivatorExpresion(method, outputVar);
            method.Statements.AddRange(this.MakeMapAssignations(input, output, outputVar, entryParam));            
            method.Statements.Add(new CodeMethodReturnStatement(outputVar));
            return method;
        }

        private CodeAssignStatement[] MakeMapAssignations(Type input, Type output, CodeVariableReferenceExpression outputVar, CodeVariableReferenceExpression entryParam)
        {
            return AreSymetricClasses(input, output) ?
                    this.MakeAssignsForSymetric(input, outputVar, entryParam) :
                    this.MakeAssignsForAsymetric(input, output, outputVar, entryParam);
        }

        /// <summary>
        /// Compare if both classes have the same properties names & types
        /// </summary>
        /// <returns></returns>
        private bool AreSymetricClasses(Type input, Type output)
        {
            var inputProperties = input.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var outputProperties = output.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var inputProperty in inputProperties)
            {
                var matchedOutputProperty = output.GetProperty(inputProperty.Name, BindingFlags.Public | BindingFlags.Instance);
                if (matchedOutputProperty == null)
                    return false;
                if (!matchedOutputProperty.PropertyType.Equals(inputProperty.PropertyType))
                    return false;
            }
            return true;
        }

        #region Symetric Algorithm              

        /// <summary>
        /// Make a collection of CodeAssigment based on Symetric algorithm
        /// </summary>
        /// <param name="outputVarRef">Reference to output code variable</param>
        /// <param name="entryParamReference">Reference to entry code parameter</param>
        /// <returns></returns>
        private CodeAssignStatement[] MakeAssignsForSymetric(Type mapType, CodeVariableReferenceExpression outputVarRef, CodeVariableReferenceExpression entryParamReference)
        {
            var inputProperties = mapType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var result = new CodeAssignStatement[inputProperties.Length];
            for (var i = 0; i < inputProperties.Length; i++)
            {
                var propertyInfo = inputProperties[i];
                var propertyAssignStatement = new CodeAssignStatement(
                    new CodePropertyReferenceExpression(outputVarRef, propertyInfo.Name),
                    new CodePropertyReferenceExpression(entryParamReference, propertyInfo.Name));
                result[i] = propertyAssignStatement;
            }
            return result;
        }

        #endregion

        private CodeAssignStatement[] MakeAssignsForAsymetric(Type input, Type output, CodeVariableReferenceExpression outputVarRef, CodeVariableReferenceExpression entryParamReference)
        {
            var outputProperties = output.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var result = new List<CodeAssignStatement>();

            for (var i = 0; i < outputProperties.Length; i++)
            {
                CodeAssignStatement propertyAssignStatement = null;
                var outputProperty = outputProperties[i];
                var inputProperty = input.GetProperty(outputProperty.Name, BindingFlags.Public | BindingFlags.Instance);
                if (inputProperty == null)
                    continue;
                if (inputProperty.PropertyType.Equals(outputProperty.PropertyType))
                {
                    propertyAssignStatement = new CodeAssignStatement(
                        new CodePropertyReferenceExpression(outputVarRef, outputProperty.Name),
                        new CodePropertyReferenceExpression(entryParamReference, inputProperty.Name));
                }
                else
                {
                    if (outputProperty.PropertyType == typeof(string))
                    {
                        propertyAssignStatement = new CodeAssignStatement(
                        new CodePropertyReferenceExpression(outputVarRef, outputProperty.Name),
                        new CodeMethodInvokeExpression(
                            new CodePropertyReferenceExpression(entryParamReference, inputProperty.Name),
                            "ToString"));
                    }
                    else if (outputProperty.PropertyType.IsCastableTo(inputProperty.PropertyType, false))
                    {
                        propertyAssignStatement = new CodeAssignStatement(
                            new CodePropertyReferenceExpression(outputVarRef, outputProperty.Name),
                            new CodeCastExpression(
                                new CodeTypeReference(outputProperty.PropertyType),
                                new CodePropertyReferenceExpression(entryParamReference, inputProperty.Name)));
                    }
                    else if (inputProperty.PropertyType.IsEnum && outputProperty.PropertyType.IsCastableTo(Enum.GetUnderlyingType(inputProperty.PropertyType), false))
                    {
                        propertyAssignStatement = new CodeAssignStatement(
                            new CodePropertyReferenceExpression(outputVarRef, outputProperty.Name),
                            new CodeCastExpression(
                                new CodeTypeReference(Enum.GetUnderlyingType(inputProperty.PropertyType)),
                                new CodePropertyReferenceExpression(entryParamReference, inputProperty.Name)));
                    }
                }
                if (propertyAssignStatement != null)
                    result.Add(propertyAssignStatement);
            }
            return result.ToArray();
        }

        private CodeAssignStatement MakePrivateInnerType(CodeVariableReferenceExpression outputVarRef)
        {
            throw new NotImplementedException();
        }
    }
}
