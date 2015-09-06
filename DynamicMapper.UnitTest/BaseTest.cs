using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicMapper.UnitTest
{
    public abstract class BaseTest
    {
        protected const long _iterationsRun = 50;
        protected ConcurrentDictionary<long, IList<string>> TraceResults;
        protected ParallelOptions _parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 5 };

        #region Utils Methods

        protected void StoreTraces(string inputToString, string outputToString, long iteration, long topIteration, long timewatchMark)
        {
            TraceResults.TryAdd(iteration, new List<string>
                {
                    String.Format("Input Entity: {0}", inputToString),
                    String.Format("Output Entity: {0}", outputToString),
                    String.Format("Iteration {0} (of {1}) Time performed {2}", iteration + 1, topIteration, timewatchMark)
                });
        }

        protected void PrintTraces()
        {
            foreach (IList<string> iterationTraces in TraceResults.Values)
                foreach (var trace in iterationTraces)
                    Trace.WriteLine(trace);
        }

        #endregion
    }
}
