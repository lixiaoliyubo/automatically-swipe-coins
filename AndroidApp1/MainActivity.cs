using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace AndroidApp1
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private readonly string[] RequiredPermissions = new[]
        {
            Android.Manifest.Permission.WriteExternalStorage,
            Android.Manifest.Permission.ReadExternalStorage
        };

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            // 检查并请求权限
            if (!CheckPermissions())
            {
                RequestPermissions(RequiredPermissions, 1);
            }
            else
            {
                StartScreenshotService();
            }
        }

        private bool CheckPermissions()
        {
            foreach (var permission in RequiredPermissions)
            {
                if (ContextCompat.CheckSelfPermission(this, permission) != Permission.Granted)
                {
                    return false;
                }
            }
            return true;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == 1)
            {
                if (grantResults.Length > 0 && grantResults.All(r => r == Permission.Granted))
                {
                    StartScreenshotService();
                }
            }
        }

        private void StartScreenshotService()
        {
            var serviceIntent = new Intent(this, typeof(ScreenshotService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                StartForegroundService(serviceIntent);
            }
            else
            {
                StartService(serviceIntent);
            }
        }
    }
}