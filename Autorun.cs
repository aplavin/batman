using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.Win32;

namespace BatMan
{
    class AutoRun
    {
        /// <summary>
        /// Full name of starting application (Shortcut)
        /// </summary>
        public static string FullPath
        {
            get { return Assembly.GetExecutingAssembly().Location; }
        }

        private static bool autoRun = false;
        private static bool registryChecked = false;
        private static string autoRunKeyName = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

        private static RegistryKey AutoRunKey
        {
            get { return Registry.CurrentUser.OpenSubKey(autoRunKeyName, true); }
        }

        /// <summary>
        /// Name of product (Shortcut)
        /// <summary>
        public static string Name { get { return "BatMan"; } }

        /// <summary>
        /// Property Enabled (static)
        /// </summary>
        public static bool Enabled
        {

            get
            {
                if (!registryChecked)
                {
                    string s = Convert.ToString(AutoRunKey.GetValue(Name, ""));
                    if (s == FullPath)
                        autoRun = true;
                    registryChecked = true;
                }
                return autoRun;
            }

            set
            {
                if (registryChecked && autoRun != value || !registryChecked)
                {
                    if (autoRun = value)
                    {
                        AutoRunKey.SetValue(Name, FullPath);
                    }
                    else
                    {
                        AutoRunKey.DeleteValue(Name, false);
                    }
                }
                registryChecked = true;
            }
        }
    }
}
