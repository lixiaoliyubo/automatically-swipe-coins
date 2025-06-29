using FFmpeg.AutoGen;
using System.Configuration;
using System.Data;
using System.Windows;

namespace AdAuto
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            ffmpeg.RootPath = "scrcpy";

            base.OnStartup(e);
        }
    }

}
