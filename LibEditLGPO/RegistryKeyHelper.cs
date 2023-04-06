using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibEditLGPO
{
    public static class RegistryKeyHelper
    {
        public static object GetPolicySetting(
            RegistryKey rootRegistryKey,
            string key,
            string valueName
        )
        {
            // Data can't be null so we can use this value to indicate key must be delete
            using (RegistryKey subKey = rootRegistryKey.OpenSubKey(key, true))
            {
                if (subKey == null)
                {
                    return null;
                }
                else
                {
                    return subKey.GetValue(valueName);
                }
            }
        }

        /// <summary>
        /// Set a policy setting
        /// </summary>
        /// <param name="rootRegistryKey">Obtained by `ComputerGroupPolicyObject.GetRootRegistryKey`</param>
        /// <param name="key">e.g. `SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU`</param>
        /// <param name="valueName">e.g. `AlwaysAutoRebootAtScheduledTime`</param>
        /// <param name="settingValue">`null` will erase registry value. (e.g. 1)</param>
        /// <param name="registryValueKind">e.g. DWord</param>
        public static void SetPolicySetting(
            RegistryKey rootRegistryKey,
            string key,
            string valueName,
            object settingValue,
            RegistryValueKind registryValueKind
        )
        {
            // Data can't be null so we can use this value to indicate key must be delete
            if (settingValue == null)
            {
                using (RegistryKey subKey = rootRegistryKey.OpenSubKey(key, true))
                {
                    if (subKey != null)
                    {
                        subKey.DeleteValue(valueName, false);
                    }
                }
            }
            else
            {
                using (RegistryKey subKey = rootRegistryKey.CreateSubKey(key))
                {
                    subKey.SetValue(valueName, settingValue, registryValueKind);
                }
            }
        }
    }
}
