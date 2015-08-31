using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DynamicMapper.Exceptions;
using DynamicMapper.TypeExtensions;

namespace DynamicMapper.MapperMaker.Factories
{
    class CodeMapMethodFactory
    {
        private const string ENTRY = "entry";
        private const string OUTPUT = "output";

        private List<CodeMemberMethod> _recursiveMethods = new List<CodeMemberMethod>();

        /// <summary>
        /// Create a method/s dynamic code required in order to map the input type to ouput type.
        /// </summary>
        /// <param name="input">The input type of the method, will be the entry parameter</param>
        /// <param name="output">The output type of the method, will be the return type statment</param>
        /// <param name="name">The name that the root method will have</param>
        /// <returns>Generated dynamic code</returns>
        public CodeMemberMethod[] Get(Type input, Type output, string name)
        {
            try
            {
                _recursiveMethods.Clear();
                var method = MakeMethod(name, input, output);
                if (_recursiveMethods.Count > 0)
                    return (new[] { method }).Concat(_recursiveMethods).ToArray();
                return new[] { method };
            }
            catch (Exception ex)
            {
                throw new MethodsMappingGenerationExcepetion(input, output, name, ex);
            }
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
            method.Name = name;
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
                CheckForbiddenTypes(inputProperty);
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
        /// Make a collection of <see cref="CodeAssignStatement"/> based on Symetric algorithm.
        /// Create the direct reference links between the properties names.
        /// If a forbidden type is detected, as a DBConnection or a COM object an excpetion will be thrown
        /// </summary>
        /// <param name="outputVarRef">Reference to output code variable</param>
        /// <param name="entryParamReference">Reference to entry code parameter</param>
        /// <returns>Collection of assign statments</returns>
        /// <remarks>This algorithm will only works if the input and the ouput type has the same number of properties and the same types for each other</remarks>
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

        #region Asymetric & Nested Algorithm

        /// <summary>
        /// Make a collection of <see cref="CodeAssignStatement"/> based on Asymetric algorithm.
        /// Create the references for each property, trying to find the same property on the destination type.
        /// This code is capable to cast certain type to antoher types:
        /// Any object to string calling his ToString method
        /// Any explicit castable object to its destination type, for example a int to short.
        /// Any similar structure Enums
        /// for a custom type the code will crate a especific method throught recursion and will be placed this method invocation as a map.
        /// If a forbidden type is detected, as a DBConnection or a COM object an excpetion will be thrown
        /// </summary>
        /// <param name="input">The input type to map</param>
        /// <param name="output">The output type to map</param>
        /// <param name="outputVarRef">Reference to output code variable</param>
        /// <param name="entryParamReference">Reference to entry code parameter</param>
        /// <returns>Collection of assign statments</returns>
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
                CheckForbiddenTypes(inputProperty);
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
                    else if (outputProperty.PropertyType.IsCastableTo(inputProperty.PropertyType,  false))
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
                    else
                    {                        
                        var innerMethodName = "To" + inputProperty.PropertyType.Name;
                        var innerMethod = this.MakeMethod(innerMethodName, inputProperty.PropertyType, outputProperty.PropertyType);
                        _recursiveMethods.Add(innerMethod);
                        propertyAssignStatement = new CodeAssignStatement(
                            new CodePropertyReferenceExpression(outputVarRef, outputProperty.Name),
                            new CodeMethodInvokeExpression(
                                new CodeMethodReferenceExpression(null, innerMethodName),                                
                                new CodePropertyReferenceExpression(entryParamReference, inputProperty.Name)));
                    }
                }
                if (propertyAssignStatement != null)
                    result.Add(propertyAssignStatement);
            }
            return result.ToArray();
        }

        private void CheckForbiddenTypes(PropertyInfo inputProperty)
        {
            if (inputProperty.PropertyType.IsCOMObject)
            {
                throw new COMObjectCastException(inputProperty.PropertyType, inputProperty.Name);
            }
            else if (inputProperty.PropertyType.IsCastableTo(typeof(System.IO.Stream), false))
            {
                throw new ForbiddenTypeCastException(typeof(System.IO.Stream), inputProperty.PropertyType, inputProperty.Name);
            }
            else if (inputProperty.PropertyType.IsCastableTo(typeof(System.Net.Sockets.Socket), false))
            {
                throw new ForbiddenTypeCastException(typeof(System.Net.Sockets.Socket), inputProperty.PropertyType, inputProperty.Name);
            }
            else if (inputProperty.PropertyType.IsCastableTo(typeof(System.Data.Common.DbConnection), false))
            {
                throw new ForbiddenTypeCastException(typeof(System.Data.Common.DbConnection), inputProperty.PropertyType, inputProperty.Name);
            }
            else if (inputProperty.PropertyType.IsCastableTo(typeof(System.IO.File), false))
            {
                throw new ForbiddenTypeCastException(typeof(System.IO.File), inputProperty.PropertyType, inputProperty.Name);
            }
            else if (inputProperty.PropertyType.IsCastableTo(typeof(System.IO.Directory), false))
            {
                throw new ForbiddenTypeCastException(typeof(System.IO.Directory), inputProperty.PropertyType, inputProperty.Name);
            }
        }

        #endregion
    }
}
