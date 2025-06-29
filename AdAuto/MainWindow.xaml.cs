using AdAuto.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace AdAuto
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm) {
                vm.StopAutomation();
                vm.Dispose();
            }

            base.OnClosing(e);
        }
    }
}