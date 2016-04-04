using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Serilog.Events;

namespace Serilog.Sinks.Seq
{
    internal class FailoverSeqSink : SeqSink
    {
        private readonly LogEventLevel _failoverRestrictedToMinimumLevel;

        private readonly Lazy<DurableSeqSink> _durableSink;

        public FailoverSeqSink(
            string serverUrl,
            string bufferBaseFilename,
            string apiKey,
            int batchPostingLimit,
            TimeSpan batchPeriod,
            long? bufferFileSizeLimitBytes,
            TimeSpan durableShipperPeriod,
            LogEventLevel failoverRestrictedToMinimumLevel)
            : base(serverUrl, apiKey, batchPostingLimit, batchPeriod)
        {
            _failoverRestrictedToMinimumLevel = failoverRestrictedToMinimumLevel;
            _durableSink =
                new Lazy<DurableSeqSink>(
                    () =>
                    new DurableSeqSink(
                        serverUrl,
                        bufferBaseFilename,
                        apiKey,
                        batchPostingLimit,
                        durableShipperPeriod,
                        bufferFileSizeLimitBytes));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && _durableSink.IsValueCreated)
            {
                _durableSink.Value.Dispose();
            }
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            try
            {
                await base.EmitBatchAsync(events);
            }
            catch (Exception)
            {
                EmitToDurableSink(events);
            }
        }

        private void EmitToDurableSink(IEnumerable<LogEvent> events)
        {
            foreach (var logEvent in events.Where(logEvent => logEvent.Level >= _failoverRestrictedToMinimumLevel))
            {
                _durableSink.Value.Emit(logEvent);
            }
        }
    }
}