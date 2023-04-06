# HiraokaHyperTools.LibEditLGPO

[![Nuget](https://img.shields.io/nuget/v/HiraokaHyperTools.LibEditLGPO)](https://www.nuget.org/packages/HiraokaHyperTools.LibEditLGPO)

This will provide Simple wrapper of IGroupPolicyObject: [IGroupPolicyObject (gpedit.h) - Win32 apps | Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/api/gpedit/nn-gpedit-igrouppolicyobject)

Links: [Doxygen](https://hiraokahypertools.github.io/LibEditLGPO/html/)

## Listing usage

Note: This must run on STA thread apartment!

```cs
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
```

Output:

```txt
SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting\DontShowUI = 0
```

## Modification usage

Note: This must run on STA thread apartment!
Note: This must run on Administrator privileges!

```cs
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
```

## Run on STA thread apartment

Main:

```cs
[STAThread()]
static int Main(string[] args)
{
  // ...
}
```

new Thread:

```cs
var t = new Thread(() =>
{
  // ...
});
t.SetApartmentState(ApartmentState.STA);
t.Start();
t.Join();
```

NUnit:

```cs
[Test]
[Apartment(ApartmentState.STA)]
public void ListUsage()
{
  // ...
}
```
