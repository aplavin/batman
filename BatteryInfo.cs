
using System;
using System.Management;
using System.Threading;
using ReflectionFacade;
using System.Reflection;
using System.Windows.Forms;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;

namespace BatMan
{
    static class BatteryInfo
    {
        // The fields are filled through Reflection, so it's OK when a warning is shown here
        private static ManagementObject BatteryStatus;
        private static ManagementObject BatteryFullChargedCapacity;
        private static ManagementObject BatteryStaticData;

        private static readonly int OffPercent = 5;

        static BatteryInfo()
        {
            //Update(null);
            //Timer t = new Timer(Update, null, 0, 5000);
        }

        public static void Update(object state)
        {
            var scope = new ManagementScope("\\\\.\\root\\WMI");
            scope.Connect();

            foreach (string className in new string[] { "BatteryStatus", "BatteryFullChargedCapacity", "BatteryStaticData" })
            {
                var query = new ObjectQuery("SELECT * FROM " + className);
                using (var searcher = new ManagementObjectSearcher(scope, query))
                {
                    foreach (ManagementObject queryObj in searcher.Get())
                    {
                        Reflector.StaticSetField(typeof(BatteryInfo), className, queryObj);
                        //typeof(BatteryInfo).GetField(className, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, queryObj);
                    }
                }
            }
        }

        #region Properties (WMI info)

        // BatteryStatus class properties
        public static bool PowerOnline
        {
            get { return SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online; }
        }
        public static bool Charging
        {
            get { return (bool)BatteryStatus.GetPropertyValue(MethodBase.GetCurrentMethod().Name.Substring(4)); }
        }
        public static int ChargeRate
        {
            get { return (int)BatteryStatus.GetPropertyValue(MethodBase.GetCurrentMethod().Name.Substring(4)); }
        }
        public static bool Discharging
        {
            get { return (bool)BatteryStatus.GetPropertyValue(MethodBase.GetCurrentMethod().Name.Substring(4)); }
        }
        public static int DischargeRate
        {
            get { return (int)BatteryStatus.GetPropertyValue(MethodBase.GetCurrentMethod().Name.Substring(4)); }
        }
        public static uint RemainingCapacity
        {
            get { return (uint)BatteryStatus.GetPropertyValue(MethodBase.GetCurrentMethod().Name.Substring(4)); }
        }
        public static uint Voltage
        {
            get { return (uint)BatteryStatus.GetPropertyValue(MethodBase.GetCurrentMethod().Name.Substring(4)); }
        }

        // BatteryFullChargedCapacity class properties
        public static uint FullChargedCapacity
        {
            get { return (uint)BatteryFullChargedCapacity.GetPropertyValue(MethodBase.GetCurrentMethod().Name.Substring(4)); }
        }

        // BatteryStaticData class properties
        public static uint DesignedCapacity
        {
            get { return (uint)BatteryStaticData.GetPropertyValue(MethodBase.GetCurrentMethod().Name.Substring(4)); }
        }
        public static string DeviceName
        {
            get { return (string)BatteryStaticData.GetPropertyValue(MethodBase.GetCurrentMethod().Name.Substring(4)); }
        }
        public static string ManufactureName
        {
            get { return (string)BatteryStaticData.GetPropertyValue(MethodBase.GetCurrentMethod().Name.Substring(4)); }
        }

        #endregion

        #region Properties (calculated)

        public static double WearPercents
        {
            get { return 100.0 * (1 + FullChargedCapacity / DesignedCapacity); }
        }

        public static double Percents
        {
            get { return 100.0 * RemainingCapacity / FullChargedCapacity; }
        }

        public static TimeSpan DischargeTimeLeft
        {
            get
            {
                try
                {
                    return Discharging ? TimeSpan.FromHours((Percents - OffPercent) / 100 * FullChargedCapacity / DischargeRate) : TimeSpan.Zero;
                }
                catch (OverflowException)
                {
                    return TimeSpan.Zero;
                }
            }
        }

        public static TimeSpan ChargeTimeLeft
        {
            get
            {
                try
                {
                    return Charging ? TimeSpan.FromHours((double)(FullChargedCapacity - RemainingCapacity) / ChargeRate) : TimeSpan.Zero;
                }
                catch (OverflowException)
                {
                    return TimeSpan.Zero;
                }
            }
        }

        public static TimeSpan TimeLeft
        {
            get { return Discharging ? DischargeTimeLeft : ChargeTimeLeft; }
        }

        #endregion

    }
}
