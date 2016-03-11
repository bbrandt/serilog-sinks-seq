using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Seq;

namespace Serilog
{
    /// <summary>
    /// Sink uses non-durable for log entries.  If exception is thrown from Emit(), then LogEntry is sent to durable log shipper.
    /// </summary>
    class FailoverSeqSink : SeqSink, IDisposable
    {
        private readonly Lazy<DurableSeqSink> _durableSink; 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="serverUrl"></param>
        /// <param name="bufferBaseFilename"></param>
        /// <param name="apiKey"></param>
        /// <param name="batchPostingLimit"></param>
        /// <param name="batchPeriod"></param>
        /// <param name="durableShipperPeriod"></param>
        /// <param name="bufferFileSizeLimitBytes"></param>
        public FailoverSeqSink(string serverUrl, string bufferBaseFilename, string apiKey, int batchPostingLimit, TimeSpan batchPeriod, TimeSpan durableShipperPeriod, long? bufferFileSizeLimitBytes)
            :base(serverUrl, apiKey, batchPostingLimit, batchPeriod)
        {
            _durableSink = new Lazy<DurableSeqSink>(() => new DurableSeqSink(serverUrl, bufferBaseFilename, apiKey, batchPostingLimit, durableShipperPeriod, bufferFileSizeLimitBytes));
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
            foreach (var logEvent in events)
            {
                _durableSink.Value.Emit(logEvent);
            }
        }
    }
}