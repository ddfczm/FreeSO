using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Utils
{
    public class StatisticsAggregator
    {
        private static TimeSpan GRANULARITY = TimeSpan.FromSeconds(30);
        private static TimeSpan RETENTION_PERIOD = TimeSpan.FromDays(7);

        private Dictionary<StatisticAggregationKey, StatisticAggregation> Aggregations;
        private List<StatisticsCollector> Collectors;

        private System.Timers.Timer DigestTimer = new System.Timers.Timer(10000);
        
        public StatisticsAggregator()
        {
            Aggregations = new Dictionary<StatisticAggregationKey, StatisticAggregation>();
            Collectors = new List<StatisticsCollector>();

            DigestTimer.Elapsed += DigestTimer_Elapsed;
        }

        private void DigestTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Digest();
        }

        public void AddCollector(StatisticsCollector collector)
        {
            Collectors.Add(collector);
        }

        public void StartDigest()
        {
            DigestTimer.Start();
        }

        public void StopDigest()
        {
            DigestTimer.Stop();
        }

        public void Digest()
        {
            foreach(var collector in Collectors)
            {
                var facts = collector.Drain();
                AggregateFacts(facts);
            }

            Purge();
        }

        public void Purge()
        {
            var window = DateTime.UtcNow.Subtract(RETENTION_PERIOD);

            var keys = Aggregations.Keys.ToList();
            foreach (var key in keys)
            {
                if (key.Time < window)
                {
                    Aggregations.Remove(key);
                }
            }
        }

        public List<StatisticAggregation> Query(StatisticsQuery query)
        {
            var matches = Aggregations.Where(x => query.Matches(x.Value));
            var grouped = matches.GroupBy(x =>
            {
                return KeyForQuery(query, x.Key);
            });

            return grouped.Select(x =>
            {
                var list = x.Select(y => y.Value).ToList();

                var first = list[0];
                var result = new StatisticAggregation()
                {
                    Key = x.Key,

                    Avg = first.Avg,
                    Count = first.Count,
                    Max = first.Max,
                    Min = first.Min,
                    Sum = first.Sum
                };

                for (var i = 1; i < list.Count; i++)
                {
                    result = Merge(result, list[i]);
                }

                return result;
            }).ToList();
        }

        private StatisticAggregationKey KeyForQuery(StatisticsQuery query, StatisticAggregationKey source)
        {
            var time = RoundDate(source.Time, query.Granularity);

            if (query.Dimensions.Count == 0)
            {
                return new StatisticAggregationKey()
                {
                    Statistic = Statistic.For(source.Statistic.MetricName),
                    Time = time
                };
            }
            else
            {
                var dims = new Dictionary<string, string>(query.Dimensions);
                foreach(var dim in dims.Keys.ToList())
                {
                    if(dims[dim] == "*"){
                        dims[dim] = source.Statistic.Dimensions[dim];
                    }
                }

                return new StatisticAggregationKey()
                {
                    Statistic = Statistic.For(source.Statistic.MetricName, dims),
                    Time = time
                };
            }
        }

        public void AggregateFacts(List<StatisticFact> values)
        {
            var aggregates = 
                values.GroupBy(x => new StatisticAggregationKey() { Time = RoundDate(x.Time, GRANULARITY), Statistic = x.Statistic }).Select(x =>
                {
                    return new StatisticAggregation()
                    {
                        Key = x.Key,
                    
                        Avg = x.Average(y => y.Value),
                        Min = x.Min(y => y.Value),
                        Max = x.Max(y => y.Value),
                        Sum = x.Sum(y => y.Value),
                        Count = x.Count()
                    };
                });
            
            foreach(var aggregate in aggregates)
            {
                if (Aggregations.ContainsKey(aggregate.Key))
                {
                    Aggregations[aggregate.Key] = Merge(Aggregations[aggregate.Key], aggregate);
                }
                else
                {
                    Aggregations.Add(aggregate.Key, aggregate);
                }
            }
        }

        private StatisticAggregation Merge(StatisticAggregation agg1, StatisticAggregation agg2)
        {
            return new StatisticAggregation
            {
                Key = agg1.Key,

                Min = Math.Min(agg1.Min, agg2.Min),
                Max = Math.Max(agg1.Max, agg2.Max),
                Avg = (agg1.Avg + agg2.Avg) / 2.0d,
                Count = agg1.Count + agg2.Count,
                Sum = agg1.Sum + agg2.Sum
            };
        }

        private DateTime RoundDate(DateTime date, TimeSpan granularit)
        {
            return new DateTime(((date.Ticks + granularit.Ticks - 1) / granularit.Ticks) * granularit.Ticks);
        }
    }

    public class StatisticsQuery
    {
        public string MetricName;
        public Dictionary<string, string> Dimensions;
        public DateTime Start;
        public DateTime End;
        public TimeSpan Granularity;
        
        public bool Matches(StatisticAggregation a)
        {
            if(a.Key.Statistic.MetricName != MetricName)
            {
                return false;
            }
            if(a.Key.Time < Start || a.Key.Time > End)
            {
                return false;
            }

            var aDims = a.Key.Statistic.Dimensions;
            foreach (var dim in Dimensions)
            {
                if (!aDims.ContainsKey(dim.Key))
                {
                    return false;
                }
                var dimValue = Dimensions[dim.Key];
                if (dimValue != "*" && aDims[dim.Key] != dimValue) {
                    return false;
                }
            }
            return true;
        }
    }

    public class StatisticAggregationKey
    {
        public Statistic Statistic;
        public DateTime Time;

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + Statistic.GetHashCode();
            hash = hash * 23 + Time.GetHashCode();
            return hash;
        }

        public override bool Equals(object obj)
        {
            var key = obj as StatisticAggregationKey;
            if(key != null)
            {
                return key.Time.Equals(Time) && key.Statistic.Equals(Statistic);
            }
            else
            {
                return false;
            }
        }
    }

    public class StatisticAggregation
    {
        public StatisticAggregationKey Key;

        public double Min;
        public double Max;
        public double Avg;
        public double Sum;
        public int Count;
    }
}
