using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamicMapper.Exceptions
{
    public class ForbiddenTypeCastException : Exception
    {
        private const string _errorMessage = "{0} of type {1} is a forbidden type, inherits {2}, Cannot map this kind of type, please consider to use same assembly {3} in both layers in order to make a direct reference assign";

        public Type ForbiddenBaseType { get; set; }
        public string PropertyTypeName { get; set; }
        public string PropertyName { get; set; }

        public ForbiddenTypeCastException(Type baseTypeDetection, Type propertyType, string propertyName)
           : base(String.Format(_errorMessage, propertyName, propertyType.Name, baseTypeDetection.Name, propertyType.Assembly.FullName))
        {
            this.ForbiddenBaseType = baseTypeDetection;
            this.PropertyTypeName = propertyType.Name;
            this.PropertyName = propertyName;
        }
    }
}
