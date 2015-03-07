using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamicMapper.Interfaces
{
    public interface ITypeMapper<TInput, TOutput> : IMapper
    {
        TOutput Map(TInput entry);
        TInput ReverseMap(TOutput entry);
    }
}
