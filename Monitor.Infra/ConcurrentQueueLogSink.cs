using Serilog.Core;
using Serilog.Events;
using System.Collections.Concurrent;

namespace Monitor.Infra.LogSink
{
    public class ConcurrentQueueLogSink : ILogEventSink
    {
        private readonly ConcurrentQueue<LogInfo> _queue;
        private readonly int _maxSize;

        public ConcurrentQueueLogSink(ConcurrentQueue<LogInfo> queue, int maxSize)
        {
            _queue = queue;
            _maxSize = maxSize;
        }

        public void Emit(LogEvent logEvent)
        {
            var logInfo = new LogInfo
            {
                Level = logEvent.Level.ToString(),
                Message = logEvent.RenderMessage(),
                Exception = logEvent.Exception != null ? logEvent.Exception.ToString() : string.Empty,
                Timestamp = logEvent.Timestamp.UtcDateTime,
                //SourceContext = logEvent.Properties.TryGetValue("SourceContext", out var sc) ? sc.ToString().Trim('"') : null
            };

            _queue.Enqueue(logInfo);

            // Trim queue if over max size
            while (_queue.Count > _maxSize && _queue.TryDequeue(out _)) { }
        }
    }
	public class LogInfo
	{
		public DateTime Timestamp { get; set; } = DateTime.Now;
		public string Level { get; set; } = string.Empty;
		//public string Identifier { get; set; } = string.Empty;
		public string Message { get; set; } = string.Empty;
		public string Exception { get; set; } = string.Empty;
	}
}
