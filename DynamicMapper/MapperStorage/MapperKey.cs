using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamicMapper.MapperStorage
{
    public class MapperKey
    {
        public Type InputType { get; set; }
        public Type OutputType { get; set; }

        public MapperKey(Type inputType, Type ouputType)
        {
            InputType = inputType;
            OutputType = ouputType;
        }

        public override bool Equals(object obj)
        {
            if (obj is MapperKey)
            {
                var toCompare = obj as MapperKey;
                return (this.InputType.Equals(toCompare.InputType) && this.OutputType.Equals(toCompare.OutputType));                    
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return InputType.GetHashCode() ^ OutputType.GetHashCode();
        }
    }
}
