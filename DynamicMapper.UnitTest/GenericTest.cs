using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicMapper.UnitTest
{
    [TestClass]
    public class GenericTest
    {        
        [TestMethod]
        [TestCategory("Symetric Algorithm")]
        public void SymetricMappingTest()
        {
            var inputEntity = new OriginDTO { Value1 = "Test1", Value2 = "Test2", Value3 = DateTime.Now, Value4 = 6547899m, Value5 = int.MaxValue };
            Stopwatch timewatch = new Stopwatch();
            for (var i = 0; i < 20; i++)
            {
                timewatch.Start();
                var mapper = MapperManager.Instance.GetMapper<OriginDTO, DestinationDTO>();
                var destination = mapper.Map(inputEntity) as DestinationDTO;
                Assert.AreEqual(inputEntity.Value1, destination.Value1);
                Assert.AreEqual(inputEntity.Value2, destination.Value2);
                timewatch.Stop();
                Trace.WriteLine(String.Format("Iteration {0} Time performed {1}", i + 1, timewatch.ElapsedMilliseconds));                
                timewatch.Reset();
            }   
            
        }
    }

    public class OriginDTO
    {
        public string Value1 { get; set; }
        public string Value2 { get; set; }
        public DateTime Value3 { get; set; }
        public decimal Value4 { get; set; }
        public int Value5 { get; set; }
    }

    public class DestinationDTO
    {
        public string Value1 { get; set; }
        public string Value2 { get; set; }
        public DateTime Value3 { get; set; }
        public decimal Value4 { get; set; }
        public int Value5 { get; set; }
    }

}
