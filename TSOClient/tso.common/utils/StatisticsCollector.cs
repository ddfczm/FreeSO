using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Common.Utils
{
    public class StatisticsCollector
    {
        private const int MAX_FACTS = 10000;
        private ConcurrentQueue<StatisticFact> Queue;

        public StatisticsCollector()
        {
            Queue = new ConcurrentQueue<StatisticFact>();
        }

        /// <summary>
        /// When benchmarked, this method took roughly 5-50 ticks to execute on i7-4770HQ. First invocation took longer but
        /// that is to be expected
        /// </summary>
        /// <param name="stat"></param>
        /// <param name="value"></param>
        public void Collect(Statistic stat, double value)
        {
            //Producing too fast!
            if (Queue.Count > MAX_FACTS) { return; }

            Queue.Enqueue(new StatisticFact { Statistic = stat, Time = DateTime.UtcNow, Value = value });
        }

        public void Collect(Statistic stat, DateTime time, double value)
        {
            //Producing too fast!
            if (Queue.Count > MAX_FACTS) { return; }

            Queue.Enqueue(new StatisticFact { Statistic = stat, Time = time, Value = value });
        }

        public List<StatisticFact> Drain()
        {
            var facts = new List<StatisticFact>();
            StatisticFact fact = null;

            //Break after max facts to stop inf loop if we are producing fast
            for (var i=0; i < MAX_FACTS; i++)
            {
                if(Queue.TryDequeue(out fact))
                {
                    facts.Add(fact);
                }
                else
                {
                    break;
                }
            }

            return facts;
        }
    }

    public class StatisticFact
    {
        public Statistic Statistic;
        public double Value;
        public DateTime Time;
    }

    public class Statistic
    {
        public string MetricName { get; internal set; }
        public Dictionary<string, string> Dimensions { get; internal set; }

        public string HashString;

        internal Statistic(string metric, Dictionary<string, string> dims)
        {
            this.MetricName = metric;
            this.Dimensions = dims;

            HashString = MetricName + ";";
            foreach(var key in dims.Keys){
                HashString += key + "=" + dims[key] + ";";
            }
        }

        public override int GetHashCode()
        {
            return HashString.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var stat = obj as Statistic;
            if (stat == null) { return false; }
            return stat.MetricName.Equals(MetricName) && stat.HashString.Equals(this.HashString);
        }

        public static Statistic For(string metric)
        {
            return new Statistic(metric, new Dictionary<string, string>());
        }

        public static Statistic For(string metric, IDictionary<string, string> dims)
        {
            return new Statistic(metric, new Dictionary<string, string>(dims));
        }
    }
}
