using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Threading;

namespace LibEditLGPO.Tests
{
    public class ComputerGroupPolicyObjectTests
    {
        [Test]
        [Apartment(ApartmentState.STA)]
        public void ListUsage()
        {
            var viewComputerConfiguration = true;
            using (var gpo = new ComputerGroupPolicyObject(new GroupPolicyObjectSettings(loadRegistryInfo: true, readOnly: true)))
            using (var registryKey = gpo.GetRootRegistryKey(viewComputerConfiguration ? GroupPolicySection.Machine : GroupPolicySection.User))
            {
                void Walk(RegistryKey key)
                {
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        using (var subKey = key.OpenSubKey(subKeyName))
                        {
                            Walk(subKey);
                        }
                    }
                    foreach (var valueName in key.GetValueNames())
                    {
                        var kind = key.GetValueKind(valueName);
                        var data = key.GetValue(valueName, "", RegistryValueOptions.DoNotExpandEnvironmentNames);
                        Console.WriteLine($"{key.Name.TrimStart('\\')}\\{valueName} = {data}");
                    }
                }

                Walk(registryKey);
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        [Ignore("TDD")]
        public void ModificationUsage()
        {
            var modifyComputerConfiguration = true;
            using (var gpo = new ComputerGroupPolicyObject(new GroupPolicyObjectSettings(loadRegistryInfo: true, readOnly: false)))
            using (var registryKey = gpo.GetRootRegistryKey(modifyComputerConfiguration ? GroupPolicySection.Machine : GroupPolicySection.User))
            {
                RegistryKeyHelper.SetPolicySetting(
                    registryKey,
                    @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU",
                    @"AlwaysAutoRebootAtScheduledTime",
                    1,
                    RegistryValueKind.DWord
                );
                RegistryKeyHelper.SetPolicySetting(
                    registryKey,
                    @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU",
                    @"AlwaysAutoRebootAtScheduledTimeMinutes",
                    15,
                    RegistryValueKind.DWord
                );

                gpo.Save();
            }
        }
    }
}