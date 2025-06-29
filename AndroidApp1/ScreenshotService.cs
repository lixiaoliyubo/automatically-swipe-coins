using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using AndroidX.Core.App;
using System;

namespace AndroidApp1
{
    [Service]
    public class ScreenshotService : Service
    {
        private const int NotificationId = 1;
        private const string ChannelId = "ScreenshotServiceChannel";
        private Timer? screenshotTimer;
        private string screenshotPath = "/storage/emulated/0/Pictures/Screenshots";

        public override IBinder? OnBind(Intent? intent)
        {
            return null;
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            CreateNotificationChannel();
            var notification = CreateNotification();
            StartForeground(NotificationId, notification);

            // 确保截图保存目录存在
            if (!Directory.Exists(screenshotPath))
            {
                Directory.CreateDirectory(screenshotPath);
            }

            // 启动定时器，每秒截图一次
            screenshotTimer = new Timer(TakeScreenshot, null, 0, 1000);

            return StartCommandResult.Sticky;
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(ChannelId, "Screenshot Service", NotificationImportance.Low)
                {
                    Description = "Running screenshot service"
                };
                var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
                notificationManager?.CreateNotificationChannel(channel);
            }
        }

        private Notification CreateNotification()
        {
            var notificationBuilder = new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle("Screenshot Service")
                .SetContentText("Taking screenshots every second")
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetOngoing(true);

            return notificationBuilder.Build();
        }

        private void TakeScreenshot(object? state)
        {
            try
            {
                // 获取当前时间作为文件名
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = System.IO.Path.Combine(screenshotPath, $"screenshot_{timestamp}.png");

                // 截取屏幕
                var view = ((Activity?)GetSystemService(WindowService))?.Window?.DecorView;
                if (view != null)
                {
                    view.DrawingCacheEnabled = true;
                    var bitmap = Bitmap.CreateBitmap(view.DrawingCache);
                    view.DrawingCacheEnabled = false;

                    using var fileStream = new FileStream(filename, FileMode.Create);
                    bitmap.Compress(Bitmap.CompressFormat.Png, 100, fileStream);
                    bitmap.Recycle();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Screenshot failed: {ex.Message}");
            }
        }

        public override void OnDestroy()
        {
            screenshotTimer?.Dispose();
            base.OnDestroy();
        }
    }
}