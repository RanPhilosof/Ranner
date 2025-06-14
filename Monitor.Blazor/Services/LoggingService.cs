using AppMonitoring.SharedTypes;
using Monitor.Blazor.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitor.Blazor.Services
{
    public interface ILoggingService
    {
        List<Tuple<long, string>> GetLogHistoryAndClear();
        long CurrentLogSequence { get; }
    }
    public class LoggingService : ILoggingService
    {
        private readonly ConcurrentQueue<Tuple<long, string>> _logHistory = new();
        private const int MaxLogLines = 100;
        private long _logSequence = 0;
        public long CurrentLogSequence => _logSequence;

        private Timer? logCheckTimer;
        private IMonitorService _monitorService;

        public LoggingService(IMonitorService monitorService)
        {
            _monitorService = monitorService;
            logCheckTimer = new Timer(FetchLogs, null, 1000, Timeout.Infinite);
        }

        public void FetchLogs(object obj)
        {
            try
            {
                var allLogs = _monitorService.GetAllVmsLogs();

                foreach (var vmLogs in allLogs)
                {
                    foreach (var log in vmLogs.Item2)
                    {
                        Log($"[{log.Timestamp:HH:mm:ss}][{vmLogs.Item1}][{log.Level}] {log.Message}"); // [{log.Identifier}]
                    }
                }

                var currentLogs =  _monitorService.GetCurrentLogs();
                foreach (var log in currentLogs)
                    Log($"[{log.Timestamp:HH:mm:ss}][{"Monitor"}][{log.Level}] {log.Message}"); // [{log.Identifier}]
            }
            catch (Exception ex)
            {
                Log($"[{DateTime.Now:HH:mm:ss}][LoggingService] - {ex.ToString()}");
            }
            finally
            {
                logCheckTimer?.Change(1000, Timeout.Infinite);
            }
        }

        public List<Tuple<long, string>> GetLogHistoryAndClear()
        {
            var logs = new List<Tuple<long, string>>(_logHistory.Count);

            while (_logHistory.TryDequeue(out Tuple<long, string> log))
                logs.Add(log);

            return logs;
        }

        private void Log(string message)
        {
            var lineNum = Interlocked.Increment(ref _logSequence);
            _logHistory.Enqueue(Tuple.Create<long, string>(lineNum, message));

            while (_logHistory.Count > MaxLogLines)
                _logHistory.TryDequeue(out _);
        }
    }
}
