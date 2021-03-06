﻿// Seq Client for .NET - Copyright 2014 Continuous IT Pty Ltd
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Seq;

namespace Serilog
{
    /// <summary>
    /// Extends Serilog configuration to write events to Seq.
    /// </summary>
    public static class SeqLoggerConfigurationExtensions
    {
        /// <summary>
        /// Adds a sink that writes log events to a http://getseq.net Seq event server.
        /// </summary>
        /// <param name="loggerSinkConfiguration">The logger configuration.</param>
        /// <param name="serverUrl">The base URL of the Seq server that log events will be written to.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required 
        /// in order to write an event to the sink.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="bufferBaseFilename">Path for a set of files that will be used to buffer events until they
        /// can be successfully transmitted across the network. Individual files will be created using the
        /// pattern <paramref name="bufferBaseFilename"/>-{Date}.json.</param>
        /// <param name="apiKey">A Seq <i>API key</i> that authenticates the client to the Seq server.</param>
        /// <param name="bufferFileSizeLimitBytes">The maximum size, in bytes, to which the buffer
        /// log file for a specific date will be allowed to grow. By default no limit will be applied.</param>
        /// <param name="eventBodyLimitBytes">The maximum size, in bytes, that the JSON representation of
        /// an event may take before it is dropped rather than being sent to the Seq server. Specify null for no limit.
        /// The default is 265 KB.</param>
        /// <param name="controlLevelSwitch">If provided, the switch will be updated based on the Seq server's level setting
        /// for the corresponding API key. Passing the same key to MinimumLevel.ControlledBy() will make the whole pipeline
        /// dynamically controlled. Do not specify <paramref name="restrictedToMinimumLevel"/> with this setting.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration Seq(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string serverUrl,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            int batchPostingLimit = SeqSink.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            string apiKey = null,
            string bufferBaseFilename = null,
            long? bufferFileSizeLimitBytes = null,
            long? eventBodyLimitBytes = 256 * 1024,
            LoggingLevelSwitch controlLevelSwitch = null)
        {
            if (loggerSinkConfiguration == null) throw new ArgumentNullException(nameof(loggerSinkConfiguration));
            if (serverUrl == null) throw new ArgumentNullException(nameof(serverUrl));
            if (bufferFileSizeLimitBytes.HasValue && bufferFileSizeLimitBytes < 0) throw new ArgumentException("Negative value provided; file size limit must be non-negative");

            var defaultedPeriod = period ?? SeqSink.DefaultPeriod;

            ILogEventSink sink;
            if (bufferBaseFilename == null)
                sink = new SeqSink(serverUrl, apiKey, batchPostingLimit, defaultedPeriod, eventBodyLimitBytes, controlLevelSwitch);
            else
                sink = new DurableSeqSink(serverUrl, bufferBaseFilename, apiKey, batchPostingLimit, defaultedPeriod, bufferFileSizeLimitBytes, eventBodyLimitBytes, controlLevelSwitch);

            return loggerSinkConfiguration.Sink(sink, restrictedToMinimumLevel);
        }
    }
}
