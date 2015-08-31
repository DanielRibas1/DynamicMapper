using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamicMapper.Exceptions
{
    public class COMObjectCastException : Exception
    {
        private const string _errorMessage = "{0} of type {1} is a COM object, Cannot map this kind of type, please consider to use same assembly {2} in both layers in order to make a direct reference assign";
        public string PropertyTypeName { get; set; }
        public string PropertyName { get; set; }

        public COMObjectCastException(Type propertyType, string propertyName)
            : base(String.Format(_errorMessage, propertyName, propertyType.Name, propertyType.Assembly.FullName))
        {
            this.PropertyTypeName = propertyType.Name;
            this.PropertyName = propertyName;
        }
    }
}
