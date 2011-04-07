
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;

namespace BatMan
{

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (TaskbarItemInfo == null)
                TaskbarItemInfo = new TaskbarItemInfo();

            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(UpdateState);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        private void UpdateState(object sender, EventArgs e)
        {
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            TaskbarItemInfo.ProgressValue = new Random().NextDouble(); // BatteryInfo.Percents / 100

            TaskbarItemInfo.Overlay = new BitmapImage(new Uri(BatteryInfo.PowerOnline ? "ac_power.png" : "battery.png", UriKind.Relative));
        }

    }
}
