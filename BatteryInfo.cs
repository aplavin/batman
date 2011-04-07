
using System;
using System.Management;
using System.Reflection;

namespace BatMan
{
    static class BatteryInfo
    {
        // The fields are filled through Reflection, so it's OK when a warning is shown here
        private static ManagementObject BatteryStatus;
        private static ManagementObject BatteryFullChargedCapacity;
        private static ManagementObject BatteryStaticData;

        static BatteryInfo()
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
                        typeof(BatteryInfo).GetField(className, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, queryObj);
                    }
                }
            }
        }

        #region Properties (WMI info)

        // BatteryStatus class properties
        public static bool PowerOnline
        {
            get { return (bool)BatteryStatus.GetPropertyValue(MethodBase.GetCurrentMethod().Name.Substring(4)); }
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

        #region Properties (countable)

        public static double WearPercents
        {
            get { return 100.0 * (DesignedCapacity - FullChargedCapacity) / DesignedCapacity; }
        }

        public static double Percents
        {
            get { return 100.0 * RemainingCapacity / FullChargedCapacity; }
        }

        public static TimeSpan DischargeTimeLeft
        {
            get { return Discharging ? TimeSpan.FromHours((double)RemainingCapacity / DischargeRate) : TimeSpan.Zero; }
        }

        public static TimeSpan ChargeTimeLeft
        {
            get { return Charging ? TimeSpan.FromHours((double)(FullChargedCapacity - RemainingCapacity) / ChargeRate) : TimeSpan.Zero; }
        }

        #endregion

    }
}
