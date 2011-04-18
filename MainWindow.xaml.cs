
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.Win32;
using BatMan.Properties;
using System.Linq;
using System.Text.RegularExpressions;

namespace BatMan
{

    public partial class MainWindow : Window
    {

#if DEBUG
        private static readonly bool DEBUG = true;
#else
        private static readonly bool DEBUG = false;
#endif

        private DispatcherTimer dispatcherTimer;

        private enum ShowMode { Always, PowerOff, PowerOn, Never };
        private ShowMode showMode = (ShowMode)Settings.Default.TaskbarShow;

        private enum PowerPlan { PowerSave, Balanced, MaxPerfromance, Unknown = 255 };
        private PowerPlan powerPlan = PowerPlan.Unknown;

        private void SwitchPowerPlan(PowerPlan plan)
        {
            p0.IsChecked = p1.IsChecked = p2.IsChecked = false;
            powerPlan = plan;
            switch (plan)
            {
                case PowerPlan.PowerSave:
                    Process.Start("powercfg", "-s SCHEME_MAX");
                    p0.IsChecked = true;
                    break;
                case PowerPlan.Balanced:
                    Process.Start("powercfg", "-s SCHEME_BALANCED");
                    p1.IsChecked = true;
                    break;
                case PowerPlan.MaxPerfromance:
                    Process.Start("powercfg", "-s SCHEME_MIN");
                    p2.IsChecked = true;
                    break;
            }
        }

        private void AutoSwitchPowerPlan()
        {
            if (Settings.Default.AutoProfile)
            {
                if (BatteryInfo.PowerOnline)
                {
                    SwitchPowerPlan(PowerPlan.Balanced);
                }
                else
                {
                    SwitchPowerPlan(PowerPlan.PowerSave);
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            foreach (object o in MainGrid.Children)
            {
                if (o is Label)
                {
                    Label label = o as Label;
                    label.Tag = label.Content;
                }
            }

            Autorun.IsChecked = Settings.Default.Autorun;
            AutoProfile.IsChecked = Settings.Default.AutoProfile;

            int r = Settings.Default.RefreshInterval;
            switch (r)
            {
                case 1:
                    t0.IsChecked = true;
                    break;
                case 5:
                    t1.IsChecked = true;
                    break;
                case 10:
                    t2.IsChecked = true;
                    break;
                case 30:
                    t3.IsChecked = true;
                    break;
                case 60:
                    t4.IsChecked = true;
                    break;
            }

            // Change relative icon paths to absolute
            foreach (JumpTask task in JumpList.GetJumpList(App.Current).JumpItems)
            {
                if (!String.IsNullOrEmpty(task.IconResourcePath) && !Path.IsPathRooted(task.IconResourcePath))
                {
                    task.IconResourcePath = Path.Combine(Directory.GetCurrentDirectory(), task.IconResourcePath);
                }
            }
            JumpList.GetJumpList(App.Current).Apply();

            SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(PowerModeChanged);

            if (TaskbarItemInfo == null)
                TaskbarItemInfo = new TaskbarItemInfo();

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(UpdateState);
            dispatcherTimer.Interval = TimeSpan.FromSeconds(DEBUG ? 1 : Settings.Default.RefreshInterval);
            dispatcherTimer.Start();

            UpdateState();
            UpdateTaskbarShow();

            AutoSwitchPowerPlan();
        }

        private void UpdateState(object sender = null, EventArgs e = null)
        {
            BatteryInfo.Update();

            Random rand = new Random();

            TaskbarItemInfo.Overlay = new BitmapImage(new Uri(BatteryInfo.PowerOnline ? "ac_power.png" : "battery.png", UriKind.Relative));

            double percent = DEBUG ? rand.NextDouble() * 100 : BatteryInfo.Percents;

            PercentLabel.Content = String.Format(PercentLabel.Tag as string, percent);

            if (BatteryInfo.PowerOnline && !DEBUG)
            {
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            }
            else
            {
                TaskbarItemInfo.ProgressState = percent <= 20 ? TaskbarItemProgressState.Error :
                    percent <= 40 ? TaskbarItemProgressState.Paused :
                    percent < 99 ? TaskbarItemProgressState.Normal :
                    TaskbarItemProgressState.None;
            }
            TaskbarItemInfo.ProgressValue = percent / 100.0;

            TimeSpan time = DEBUG ? new TimeSpan((long)rand.Next() << 32 + rand.Next()) : BatteryInfo.TimeLeft;
            TimeLabel.Content = (time != TimeSpan.Zero ?
                String.Format(TimeLabel.Tag as string, time) :
                "От сети");

            CapacityLabel.Content = String.Format("Ёмкость: {0:##.0} Вт*ч из {1:##.0} Вт*ч", BatteryInfo.RemainingCapacity / 1000.0, BatteryInfo.FullChargedCapacity / 1000.0);

            if (DEBUG)
            {
                PowerLabel.Content = rand.Next(2) == 0 ?
                    String.Format("Разрядка: {0:##.0} Вт", rand.Next(50000) / 1000.0) :
                    rand.Next(2) == 0 ?
                    String.Format("Зарядка: {0:##.0} Вт", rand.Next(50000) / 1000.0) :
                    "";
            }
            else
            {
                PowerLabel.Content = BatteryInfo.Discharging ?
                    String.Format("Разрядка: {0:##.0} Вт", BatteryInfo.DischargeRate / 1000.0) :
                    BatteryInfo.Charging ?
                    String.Format("Зарядка: {0:##.0} Вт", BatteryInfo.ChargeRate / 1000.0) :
                    "";
            }

            double voltage = DEBUG ? rand.NextDouble() * 12 : BatteryInfo.Voltage / 1000.0;
            VoltageLabel.Content = String.Format(VoltageLabel.Tag as string, voltage);

            double wearLevel = DEBUG ? rand.NextDouble() * 50 : BatteryInfo.WearPercents;
            WearLevelLabel.Content = String.Format(WearLevelLabel.Tag as string, wearLevel, BatteryInfo.DesignedCapacity / 1000.0);

            PercentLabelTray.Content = PercentLabel.Content;
            TimeLabelTray.Content = TimeLabel.Content;
            CapacityLabelTray.Content = CapacityLabel.Content;
            PowerLabelTray.Content = PowerLabel.Content;
        }

        private void SetTime(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem))
            {
                throw new ArgumentException("Sender must be a MenuItem");
            }

            int secs = int.Parse((sender as MenuItem).Tag as string);
            dispatcherTimer.Interval = TimeSpan.FromSeconds(secs);

            Settings.Default.RefreshInterval = secs;

            t0.IsChecked = t1.IsChecked = t2.IsChecked = t3.IsChecked = t4.IsChecked = false;
            (sender as MenuItem).IsChecked = true;
        }

        private void SetTaskbarShow(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem))
            {
                throw new ArgumentException("Sender must be a MenuItem");
            }

            i0.IsChecked = i1.IsChecked = i2.IsChecked = i3.IsChecked = false;
            (sender as MenuItem).IsChecked = true;

            showMode = (ShowMode)int.Parse((sender as MenuItem).Tag as string);
            Settings.Default.TaskbarShow = (int)showMode;

            UpdateTaskbarShow();
        }

        private void UpdateTaskbarShow()
        {
            if (ShowTaskbar(BatteryInfo.PowerOnline))
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        private bool ShowTaskbar(bool powerOn)
        {
            return showMode == ShowMode.Always ? true :
                showMode == ShowMode.PowerOn ? powerOn :
                showMode == ShowMode.PowerOff ? !powerOn :
                false;
        }

        private void SetProfile(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem))
            {
                throw new ArgumentException("Sender must be a MenuItem");
            }

            p0.IsChecked = p1.IsChecked = p2.IsChecked = false;
            (sender as MenuItem).IsChecked = true;
            SwitchPowerPlan((PowerPlan)int.Parse((sender as MenuItem).Tag.ToString()));
        }

        private void PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            UpdateState();
            AutoSwitchPowerPlan();
            UpdateTaskbarShow();
        }

        private void Autorun_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.Autorun = Autorun.IsChecked;
            AutoRun.Enabled = Autorun.IsChecked;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();
            App.Current.Shutdown();
        }

        private void AutoProfile_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.AutoProfile = AutoProfile.IsChecked;
            if (AutoProfile.IsChecked)
            {
                AutoSwitchPowerPlan();
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Top = Left = -1000;
                WindowState = WindowState.Normal;
                UpdateTaskbarShow();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Top = Left = -1000;
            e.Cancel = true;
            UpdateTaskbarShow();
        }

        private void AdvancedInfoItem_Click(object sender, RoutedEventArgs e)
        {
            Top = Left = 150;
            Show();
            Activate();
        }

    }
}
