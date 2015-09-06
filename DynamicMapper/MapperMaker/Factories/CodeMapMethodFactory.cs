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

        private CodeMemberMethod MakeMethod(string name, Type input, Type output)
        {
            var entryParamDeclaration = new CodeParameterDeclarationExpression(new CodeTypeReference(input), ENTRY);
            var method = this.MakeBody(name, entryParamDeclaration, output);
            method.Name = name;
            var entryParam = new CodeVariableReferenceExpression(ENTRY);
            var outputVar = this.CreateNewDeclaration(method, output, OUTPUT);
            method.Statements.Add(this.CreateActivatorExpresion(method.ReturnType, outputVar));
            method.Statements.AddRange(this.MakeMapAssignations(input, output, outputVar, entryParam));            
            method.Statements.Add(new CodeMethodReturnStatement(outputVar));
            return method;
        }

        private CodeStatementCollection MakeMapAssignations(Type input, Type output, CodeExpression outputVar, CodeExpression entryParam)
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
        private CodeStatementCollection MakeAssignsForSymetric(Type mapType, CodeExpression outputVarRef, CodeExpression entryParamReference)
        {
            var inputProperties = mapType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var result = new CodeStatementCollection();
            for (var i = 0; i < inputProperties.Length; i++)
            {
                var propertyInfo = inputProperties[i];
                var inputValueRef = new CodePropertyReferenceExpression(entryParamReference, propertyInfo.Name);
                var outputValueRef = new CodePropertyReferenceExpression(outputVarRef, propertyInfo.Name);
                
                if (propertyInfo.PropertyType.IsArray)
                {
                    result.AddRange(MakeArrayAssign(propertyInfo.PropertyType.GetElementType(), propertyInfo.PropertyType.GetElementType(), outputValueRef, inputValueRef));
                }
                else if (propertyInfo.PropertyType.GetInterface("IList", false) != null)
                {
                    result.Add(CreateActivatorExpresion(new CodeTypeReference(propertyInfo.PropertyType), outputValueRef));
                    result.AddRange(
                        MakeCollectionAssign(
                        propertyInfo.PropertyType.GetGenericArguments().First(), 
                        propertyInfo.PropertyType.GetGenericArguments().First(), 
                        outputValueRef, 
                        inputValueRef,
                        new CodePropertyReferenceExpression(inputValueRef, "Count")));                        
                }                
                else
                {
                    var propertyAssignStatement = new CodeAssignStatement(
                        outputValueRef,
                        inputValueRef);
                    result.Add(propertyAssignStatement);
                }
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
        private CodeStatementCollection MakeAssignsForAsymetric(Type input, Type output, CodeExpression outputVarRef, CodeExpression entryParamReference)
        {
            var outputProperties = output.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var result = new CodeStatementCollection();

            for (var i = 0; i < outputProperties.Length; i++)
            {        
                var outputProperty = outputProperties[i];
                var inputProperty = input.GetProperty(outputProperty.Name, BindingFlags.Public | BindingFlags.Instance);
                if (inputProperty == null)
                    continue;                
                CheckForbiddenTypes(inputProperty);

                var inputValueRef = new CodePropertyReferenceExpression(entryParamReference, inputProperty.Name);
                var outputValueRef = new CodePropertyReferenceExpression(outputVarRef, outputProperty.Name);    
                
                if (inputProperty.PropertyType.IsArray)
                {
                    if (!outputProperty.PropertyType.IsArray)
                    {
                        throw new MissmatchArrayAssignException(inputProperty.PropertyType, outputProperty.PropertyType, inputProperty.Name, input);
                    }
                    result.AddRange(MakeArrayAssign(inputProperty.PropertyType.GetElementType(), outputProperty.PropertyType.GetElementType(), outputValueRef, inputValueRef));
                }
                else if (inputProperty.PropertyType.GetInterface("IList", false) != null)
                {
                    if (outputProperty.PropertyType.GetInterface("IList", false) == null)
                    {
                        throw new MissmatchArrayAssignException(inputProperty.PropertyType.GetGenericArguments().First(), outputProperty.PropertyType.GetGenericArguments().First(), inputProperty.Name, input);
                    }
                    result.Add(CreateActivatorExpresion(new CodeTypeReference(outputProperty.PropertyType), outputValueRef));
                    result.AddRange(
                        MakeCollectionAssign(
                        inputProperty.PropertyType, 
                        outputProperty.PropertyType, 
                        outputValueRef, 
                        inputValueRef,
                        new CodePropertyReferenceExpression(inputValueRef, "Count")));                            
                }      
                else if (inputProperty.PropertyType.Equals(outputProperty.PropertyType))
                {
                    result.Add(new CodeAssignStatement(
                        outputValueRef,
                        inputValueRef));
                }
                else
                {
                    if (outputProperty.PropertyType == typeof(string))
                    {
                        result.Add(new CodeAssignStatement(
                        outputValueRef,
                        new CodeMethodInvokeExpression(
                            inputValueRef,
                            "ToString")));
                    }
                    else if (outputProperty.PropertyType.IsCastableTo(inputProperty.PropertyType,  false))
                    {
                        result.Add(new CodeAssignStatement(
                            outputValueRef,
                            new CodeCastExpression(
                                new CodeTypeReference(outputProperty.PropertyType),
                                inputValueRef)));
                    }
                    else if (inputProperty.PropertyType.IsEnum && outputProperty.PropertyType.IsCastableTo(Enum.GetUnderlyingType(inputProperty.PropertyType), false))
                    {
                        result.Add(new CodeAssignStatement(
                            outputValueRef,
                            new CodeCastExpression(
                                new CodeTypeReference(Enum.GetUnderlyingType(inputProperty.PropertyType)),
                                inputValueRef)));
                    }
                    else
                    {                        
                        var innerMethodName = "To" + inputProperty.PropertyType.Name;
                        var innerMethod = this.MakeMethod(innerMethodName, inputProperty.PropertyType, outputProperty.PropertyType);
                        _recursiveMethods.Add(innerMethod);
                        result.Add(new CodeAssignStatement(
                            outputValueRef,
                            new CodeMethodInvokeExpression(
                                new CodeMethodReferenceExpression(null, innerMethodName),
                                inputValueRef)));
                    }
                }                
            }
            return result;
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

        private CodeVariableReferenceExpression CreateNewDeclaration(CodeMemberMethod method, Type varType, string varName)
        {
            method.Statements.Add(new CodeVariableDeclarationStatement(varType, varName));
            var reference = new CodeVariableReferenceExpression(varName);
            method.UserData[varName] = reference;
            return reference;
        }

        private CodeAssignStatement CreateActivatorExpresion(CodeTypeReference activatorType, CodeExpression assignVar)
        {
            var activatorReference = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(typeof(System.Activator)), "CreateInstance");
            activatorReference.TypeArguments.Add(activatorType);
            return
                new CodeAssignStatement(
                    assignVar,
                    new CodeMethodInvokeExpression(activatorReference));
        }

        private CodeStatementCollection MakeArrayAssign(Type inputChildType, Type outputChildType, CodeExpression outputValueRef, CodeExpression inputValueRef)
        {
            var inputArraySize = new CodePropertyReferenceExpression(inputValueRef, "Length");
            var arrayInit = new CodeAssignStatement(outputValueRef, new CodeArrayCreateExpression(outputChildType, inputArraySize));
            var iLoopVar = new CodeVariableDeclarationStatement(typeof(int), "i", new CodePrimitiveExpression(0));
            var iLoopVarRef = new CodeVariableReferenceExpression("i");
            
            var result = new CodeStatementCollection();
            result.Add(iLoopVar);
            result.Add(arrayInit);

            var forLoopAssign = new CodeIterationStatement(
                new CodeAssignStatement(iLoopVarRef, new CodePrimitiveExpression(0)),
                new CodeBinaryOperatorExpression(iLoopVarRef, CodeBinaryOperatorType.LessThan, inputArraySize),
                new CodeAssignStatement(iLoopVarRef, new CodeBinaryOperatorExpression(iLoopVarRef, CodeBinaryOperatorType.Add, new CodePrimitiveExpression(1))));  
            forLoopAssign.Statements.Add(new CodeAssignStatement(new CodeArrayIndexerExpression(outputValueRef, iLoopVarRef), new CodeArrayIndexerExpression(inputValueRef, iLoopVarRef)));
            result.Add(forLoopAssign);
            return result;
        }

        private CodeStatementCollection MakeCollectionAssign(Type inputChildType, Type outputChildType, CodeExpression outputValueRef, CodeExpression inputValueRef, CodeExpression collectionLength)
        {            
            var iLoopVar = new CodeVariableDeclarationStatement(typeof(int), "i", new CodePrimitiveExpression(0));
            var iLoopVarRef = new CodeVariableReferenceExpression("i");
            var innerAssign = new CodeStatementCollection(); 

            var forLoopAssign = new CodeIterationStatement(
                new CodeAssignStatement(iLoopVarRef, new CodePrimitiveExpression(0)),
                new CodeBinaryOperatorExpression(iLoopVarRef, CodeBinaryOperatorType.LessThan, collectionLength),
                new CodeAssignStatement(iLoopVarRef, new CodeBinaryOperatorExpression(iLoopVarRef, CodeBinaryOperatorType.Add, new CodePrimitiveExpression(1))));  

            if (inputChildType.IsPrimitive || inputChildType == typeof(string))
            {
                forLoopAssign.Statements.Add(new CodeMethodInvokeExpression(outputValueRef, "Add", new CodeArrayIndexerExpression(inputValueRef, iLoopVarRef)));                   
            }
            else
            {
                forLoopAssign.Statements.AddRange(MakeMapAssignations(inputChildType, outputChildType,
                    new CodeArrayIndexerExpression(outputValueRef, iLoopVarRef),
                    new CodeArrayIndexerExpression(inputValueRef, iLoopVarRef)));
            }           
           
            return new CodeStatementCollection { iLoopVar, forLoopAssign };           
        }
    }
}
