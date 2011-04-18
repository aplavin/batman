
using System;
using System.Management;
using System.Reflection;
using System.Windows;
using ReflectionFacade;

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
            Update();
        }

        public static void Update()
        {
            var scope = new ManagementScope("\\\\.\\root\\WMI");
            scope.Connect();

            foreach (string className in new string[] { "BatteryStatus", "BatteryFullChargedCapacity", "BatteryStaticData" })
            {
                var query = new ObjectQuery("SELECT * FROM " + className);
                using (var searcher = new ManagementObjectSearcher(scope, query))
                {
                    var en = searcher.Get().GetEnumerator();
                    en.MoveNext();
                    Reflector.StaticSetField(typeof(BatteryInfo), className, en.Current);
                }
            }
        }

        #region Properties (WMI info)

        // BatteryStatus class properties
        public static bool PowerOnline
        {
            get { return SystemParameters.PowerLineStatus == PowerLineStatus.Online; }
        }
        public static bool Charging
        {
            get { return (bool)BatteryStatus.GetPropertyValue("Charging"); }
        }
        public static int ChargeRate
        {
            get { return (int)BatteryStatus.GetPropertyValue("ChargeRate"); }
        }
        public static bool Discharging
        {
            get { return !PowerOnline; }
        }
        public static int DischargeRate
        {
            get { return (int)BatteryStatus.GetPropertyValue("DischargeRate"); }
        }
        public static uint RemainingCapacity
        {
            get { return (uint)BatteryStatus.GetPropertyValue("RemainingCapacity"); }
        }
        public static uint Voltage
        {
            get { return (uint)BatteryStatus.GetPropertyValue("Voltage"); }
        }

        // BatteryFullChargedCapacity class properties
        public static uint FullChargedCapacity
        {
            get { return (uint)BatteryFullChargedCapacity.GetPropertyValue("FullChargedCapacity"); }
        }

        // BatteryStaticData class properties
        public static uint DesignedCapacity
        {
            get { return (uint)BatteryStaticData.GetPropertyValue("DesignedCapacity"); }
        }
        public static string DeviceName
        {
            get { return (string)BatteryStaticData.GetPropertyValue("DeviceName"); }
        }
        public static string ManufactureName
        {
            get { return (string)BatteryStaticData.GetPropertyValue("ManufactureName"); }
        }

        #endregion

        #region Properties (calculated)

        public static double WearPercents
        {
            get { return 100.0 * (1 - (double)FullChargedCapacity / DesignedCapacity); }
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
