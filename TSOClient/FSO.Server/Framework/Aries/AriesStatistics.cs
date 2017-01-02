using FSO.Common.Utils;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Framework.Aries
{
    public class AriesStatistics : StatisticsCollector
    {
        private Statistic _MessageReceived;
        private Statistic _SessionOpened;
        private Statistic _SessionClosed;
        private Statistic _MessageSent;

        public AriesStatistics(string callSign, StatisticsAggregator aggregator)
        {
            var dimensions = new Dictionary<string, string>()
            {
                { "Host",  callSign }
            };

            _MessageReceived = Statistic.For("MessageReceived", dimensions);
            _SessionOpened = Statistic.For("SessionOpened", dimensions);
            _SessionClosed = Statistic.For("SessionClosed", dimensions);
            _MessageSent = Statistic.For("MessageSent", dimensions);

            aggregator.AddCollector(this);
        }

        public void MessageReceived()
        {
            Collect(_MessageReceived, 1);
        }

        public void SessionOpened()
        {
            Collect(_SessionOpened, 1);
        }

        public void SessionClosed()
        {
            Collect(_SessionClosed, 1);
        }

        public void MessageSent()
        {
            Collect(_MessageSent, 1);
        }
    }

    public class AriesStatisticsModule : NinjectModule
    {
        public override void Load()
        {
            Bind<AriesStatistics>().ToSelf().InSingletonScope();
        }
    }
}
