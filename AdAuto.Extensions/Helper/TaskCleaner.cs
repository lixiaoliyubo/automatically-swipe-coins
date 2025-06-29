using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace AdAuto.Extensions.Helper
{
    public static class TaskCleaner
    {
        private static bool _isRunning;
        
        public static void StartDailyCleanup(ConcurrentDictionary<string, List<string>> taskDictionary, CancellationToken cancellationToken)
        {
            if (_isRunning) return;
            
            _isRunning = true;
            
            Task.Run(async () =>
            {
                DateTime lastCleanupDate = DateTime.MinValue;
                
                while (!cancellationToken.IsCancellationRequested)
                {
                    var now = DateTime.Now;
                    
                    // 只在日期变更时执行清理，而不是每分钟检查
                    if (now.Date > lastCleanupDate.Date)
                    {
                        // 只在凌晨0点附近执行清理
                        if (now.Hour == 0)
                        {
                            foreach (var key in taskDictionary.Keys)
                            {
                                if (key != "切换账号") {
                                    taskDictionary[key].Clear();
                                }
                            }
                            lastCleanupDate = now;
                        }
                    }
                    
                    // 减少检查频率，每10分钟检查一次
                    await Task.Delay(TimeSpan.FromMinutes(10));
                }
            }, cancellationToken);
        }
    }
}