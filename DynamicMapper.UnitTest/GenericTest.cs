using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DynamicMapper.UnitTest
{
    [TestClass]
    public class GenericTest : BaseTest
    {     
        [TestInitialize]
        public void TestInitialize()
        {
            base.TraceResults = new ConcurrentDictionary<long, IList<string>>();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            base.PrintTraces();
        }

        [TestMethod]
        [TestCategory("Symetric Algorithm")]
        public void SymetricMappingTest()
        {
            var inputEntity = new OriginDTO { Value1 = "Test1", Value2 = "Test2", Value3 = DateTime.Now, Value4 = 6547899d, Value5 = int.MaxValue };
            Stopwatch timewatch = new Stopwatch();
            Trace.WriteLine(String.Format("Start Test {0}", DateTime.Now));
            Parallel.For(0, _iterationsRun, _parallelOptions, (i) =>
            {
                timewatch.Start();
                var mapper = MapperManager.Instance.GetMapper<OriginDTO, DestinationDTO>();
                var destination = mapper.Map(inputEntity) as DestinationDTO;                
                timewatch.Stop();
                StoreTraces(inputEntity.ToString(), destination.ToString(), i, _iterationsRun, timewatch.ElapsedMilliseconds);
                timewatch.Reset();
            });
        }

        [TestMethod]
        [TestCategory("Asymetric Algorithm")]
        public void SimpleAsymetricMappingTest()
        {
            var inputEntity = new AsymOriginDTO { Value1 = "Test1", Value2 = DateTime.Now, Value3 = DateTime.MaxValue, Value4 = 6547899m, Value5 = EnumDTO.Second };
            Stopwatch timewatch = new Stopwatch();
            Trace.WriteLine(String.Format("Start Test {0}", DateTime.Now));
            Parallel.For(0, _iterationsRun, _parallelOptions, (i) =>
            {
                timewatch.Start();
                var mapper = MapperManager.Instance.GetMapper<AsymOriginDTO, DestinationDTO>();
                var destination = mapper.Map(inputEntity) as DestinationDTO;
                timewatch.Stop();
                StoreTraces(inputEntity.ToString(), destination.ToString(), i, _iterationsRun, timewatch.ElapsedMilliseconds);
                timewatch.Reset();
            });
        }

        [TestMethod]
        [TestCategory("Nested Algorithm")]
        public void NestedMappingTest()
        {
            var inputEntity = new NestedOriginDTO { Name = "NestedTest", NestedDTO = new NestedSubOriginDTO { Number = 100 } };
            Stopwatch timewatch = new Stopwatch();
            Trace.WriteLine(String.Format("Start Test {0}", DateTime.Now));
            Parallel.For(0, _iterationsRun, _parallelOptions, (i) =>
            {
                timewatch.Start();
                var mapper = MapperManager.Instance.GetMapper<NestedOriginDTO, NestedDestinationDTO>();
                var destination = mapper.Map(inputEntity) as NestedDestinationDTO;
                timewatch.Stop();
                StoreTraces(inputEntity.ToString(), destination.ToString(), i, _iterationsRun, timewatch.ElapsedMilliseconds);
                timewatch.Reset();
            });
        }        
    }
    
    #region Simple DTOs

    public class OriginDTO
    {
        public string Value1 { get; set; }
        public string Value2 { get; set; }
        public DateTime Value3 { get; set; }
        public double Value4 { get; set; }
        public int Value5 { get; set; }

        public override string ToString()
        {
            return String.Format("Value1 = {0}, Value2 = {1}, Value3 = {2}, Value4 = {3}, Value5 = {4}", Value1, Value2, Value3, Value4, Value5);
        }
    }

    public class DestinationDTO
    {
        public string Value1 { get; set; }
        public string Value2 { get; set; }
        public DateTime Value3 { get; set; }
        public double Value4 { get; set; }
        public int Value5 { get; set; }

        public override string ToString()
        {
            return String.Format("Value1 = {0}, Value2 = {1}, Value3 = {2}, Value4 = {3}, Value5 = {4}", Value1, Value2, Value3, Value4, Value5);
        }
    }

    #endregion

    #region Special DTOs

    public enum EnumDTO
    {
        First = 1,
        Second = 2
    }

    public class AsymOriginDTO
    {
        public string Value1 { get; set; }
        public DateTime Value2 { get; set; }
        public DateTime Value3 { get; set; }
        public decimal Value4 { get; set; }
        public EnumDTO Value5 { get; set; }

        public override string ToString()
        {
            return String.Format("Value1 = {0}, Value2 = {1}, Value3 = {2}, Value4 = {3}, Value5 = {4}", Value1, Value2, Value3, Value4, Value5);
        }
    }

    #endregion

    #region Nested DTOs

    public class NestedOriginDTO
    {
        public string Name { get; set; }
        public NestedSubOriginDTO NestedDTO { get; set; }

        public override string ToString()
        {
            return String.Format("Name = {0}, NestedDTO = [{1}]", this.Name, this.NestedDTO.ToString());
        } 
    }

    public class NestedDestinationDTO
    {
        public string Name { get; set; }
        public NestedSubDestinationDTO NestedDTO { get; set; }

        public override string ToString()
        {
            return String.Format("Name = {0}, NestedDTO = [{1}]", this.Name, this.NestedDTO.ToString());
        } 
    }

    public class NestedSubOriginDTO
    {
        public int Number { get; set; }

        public override string ToString()
        {
 	         return String.Format("Number = {0}", this.Number);
        }
    }

    public class NestedSubDestinationDTO
    {
        public int Number { get; set; }

        public override string ToString()
        {
            return String.Format("Number = {0}", this.Number);
        }
    }


    #endregion   

}
