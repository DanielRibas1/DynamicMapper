using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicMapper.UnitTest
{
    [TestClass]
    public class CollectionsTest : BaseTest
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
        [TestCategory("With Arrays")]
        public void ArrayMappingTest()
        {            
            var inputEntity = new WithArrayOriginDTO { Value1 = 5, Value2 = new int[] { 0,1,2,3,4,5,6 }, Value3 = "Test" };            
            Stopwatch timewatch = new Stopwatch();
            Trace.WriteLine(String.Format("Start Test {0}", DateTime.Now));
            Parallel.For(0, _iterationsRun, _parallelOptions, (i) =>
            {
                timewatch.Start();
                var mapper = MapperManager.Instance.GetMapper<WithArrayOriginDTO, WithArrayDestinationDTO>();
                var destination = mapper.Map(inputEntity) as WithArrayDestinationDTO;
                timewatch.Stop();
                StoreTraces(inputEntity.ToString(), destination.ToString(), i, _iterationsRun, timewatch.ElapsedMilliseconds);
                timewatch.Reset();
            });
        }

        [TestMethod]
        [TestCategory("With Lists")]
        public void ListMappingTest()
        {
            var inputEntity = new WithListOriginDTO { Value1 = 5, Value2 = new List<string> { "One", "Two" }, Value3 = "Test" };
            Stopwatch timewatch = new Stopwatch();
            Trace.WriteLine(String.Format("Start Test {0}", DateTime.Now));
            Parallel.For(0, _iterationsRun, _parallelOptions, (i) =>
            {
                timewatch.Start();
                var mapper = MapperManager.Instance.GetMapper<WithListOriginDTO, WithListDestinationDTO>();
                var destination = mapper.Map(inputEntity) as WithListDestinationDTO;
                timewatch.Stop();
                StoreTraces(inputEntity.ToString(), destination.ToString(), i, _iterationsRun, timewatch.ElapsedMilliseconds);
                timewatch.Reset();
            });
        }      

        #region DTO

        public class WithArrayOriginDTO
        {
            public int Value1 { get; set; }
            public int[] Value2 { get; set; }
            public string Value3 { get; set; }

            public override string ToString()
            {
                return String.Format("Value1 = {0}, Value2 = {1}, Value3 = {2}", Value1, String.Join(", ", Value2), Value3);
            }
        }

        public class WithArrayDestinationDTO
        {
            public int Value1 { get; set; }
            public int[] Value2 { get; set; }
            public string Value3 { get; set; }

            public override string ToString()
            {
                return String.Format("Value1 = {0}, Value2 = {1}, Value3 = {2}", Value1, String.Join(", ", Value2), Value3);
            }
        }

        public class WithListOriginDTO
        {
            public int Value1 { get; set; }
            public List<string> Value2 { get; set; }
            public string Value3 { get; set; }

            public override string ToString()
            {
                return String.Format("Value1 = {0}, Value2 = {1}, Value3 = {2}", Value1, String.Join(", ", Value2), Value3);
            }
        }

        public class WithListDestinationDTO
        {
            public int Value1 { get; set; }
            public List<string> Value2 { get; set; }
            public string Value3 { get; set; }

            public override string ToString()
            {
                return String.Format("Value1 = {0}, Value2 = {1}, Value3 = {2}", Value1, String.Join(", ", Value2), Value3);
            }
        }

        #endregion
    }
}
