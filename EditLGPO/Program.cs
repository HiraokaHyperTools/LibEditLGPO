using CommandLine;
using EditLGPO.Helpers;
using LibEditLGPO;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EditLGPO
{
    internal class Program
    {
        [Verb("batch", HelpText = "Perform batch modification to GPO RegistryExtension.\nA policy refresh will be automatically triggered after modification.")]
        class BatchOpt
        {
            [Option('m', "machine", HelpText = "Modify Computer section, otherwise modify User section")]
            public bool Machine { get; set; }

            [Option('a', "apply", HelpText = "One or more commands to apply.\n"
                + "\n"
                + "To add or update:\n"
                + "- \"Key;ValueName;Kind;;Data\"\n"
                + "- \"SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU;AlwaysAutoRebootAtScheduledTime;4;;01-00-00-00\"\n"
                + "- \"SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU;AlwaysAutoRebootAtScheduledTime;4:text;;1\"\n"
                + "\n"
                + "The format of Kind is an integer or integer plus \":filter1:filter2::filter3...\".\n"
                + "The filter is EditLGPO specific feature. Only \":text\" filter is implemented.\n"
                + "\n"
                + "To delete:\n"
                + "- \"Key;ValueName;;;\"\n"
                + "- \"SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU;AlwaysAutoRebootAtScheduledTime;;;\"\n"
                , Max = 9999)
            ]
            public IEnumerable<string> ApplyList { get; set; }
        }

        [Verb("list", HelpText = "Enumerate configured settings from GPO RegistryExtension")]
        class ListOpt
        {
            [Option('m', "machine", HelpText = "Browse Computer section, otherwise browse User section")]
            public bool Machine { get; set; }
        }

        [STAThread()]
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<BatchOpt, ListOpt>(args)
                .MapResult<BatchOpt, ListOpt, int>(
                    DoBatch,
                    DoList,
                    ex => 1
                );
        }

        private static int DoList(ListOpt arg)
        {
            using (var gpo = new ComputerGroupPolicyObject(new GroupPolicyObjectSettings(true, true)))
            using (var registryKey = gpo.GetRootRegistryKey(arg.Machine ? GroupPolicySection.Machine : GroupPolicySection.User))
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
                        var bytes = RegistryValueToBytes(key.GetValue(valueName, "", RegistryValueOptions.DoNotExpandEnvironmentNames));
                        Console.WriteLine($"[{key.Name.TrimStart('\\')};{valueName};{(int)kind};{bytes.Length};{BitConverter.ToString(bytes)}]");
                    }
                }

                Walk(registryKey);
            }
            return 0;
        }

        private static byte[] RegistryValueToBytes(object any)
        {
            if (any is string text)
            {
                return Encoding.Unicode.GetBytes(text);
            }
            else if (any is int intValue)
            {
                var temp = new MemoryStream(new byte[4]);
                new BinaryWriter(temp).Write(intValue);
                return temp.ToArray();
            }
            else if (any is long longValue)
            {
                var temp = new MemoryStream(new byte[8]);
                new BinaryWriter(temp).Write(longValue);
                return temp.ToArray();
            }
            else if (any is string[] stringArray)
            {
                var temp = new MemoryStream();
                var writer = new BinaryWriter(temp);
                foreach (var one in stringArray)
                {
                    writer.Write(Encoding.Unicode.GetBytes(one));
                    writer.Write((ushort)0);
                }
                return temp.ToArray();
            }
            else if (any is byte[] bytea)
            {
                return bytea;
            }
            return new byte[0];
        }

        private static object BytesToRegistryValue(byte[] bytes, RegistryValueKind kind)
        {
            switch (kind)
            {
                case RegistryValueKind.DWord:
                    {
                        return new BinaryReader(new MemoryStream(bytes, false)).ReadInt32();
                    }
                case RegistryValueKind.QWord:
                    {
                        return new BinaryReader(new MemoryStream(bytes, false)).ReadInt64();
                    }
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                    {
                        return Encoding.Unicode.GetString(bytes);
                    }
                case RegistryValueKind.MultiString:
                    {
                        return Encoding.Unicode.GetString(bytes).Split('\0');
                    }
                case RegistryValueKind.Binary:
                    {
                        return (bytes);
                    }
                default:
                    throw new NotSupportedException(kind + "");
            }
        }

        private static int DoBatch(BatchOpt arg)
        {
            using (var gpo = new ComputerGroupPolicyObject(new GroupPolicyObjectSettings(loadRegistryInfo: true, readOnly: false)))
            using (var registryKey = gpo.GetRootRegistryKey(arg.Machine ? GroupPolicySection.Machine : GroupPolicySection.User))
            {
                if (arg.ApplyList != null)
                {
                    foreach (var item in arg.ApplyList)
                    {
                        var parsed = new ApplyItemParser(item);
                        if (parsed.Kind.HasValue)
                        {
                            var toObject = parsed.HasTextFilter ? (ToObject)TextToObject : HexToObject;
                            var kind = (RegistryValueKind)parsed.Kind;
                            RegistryKeyHelper.SetPolicySetting(
                                registryKey,
                                parsed.Key,
                                parsed.ValueName,
                                toObject(parsed.Data, kind),
                                kind
                            );
                        }
                        else
                        {
                            RegistryKeyHelper.SetPolicySetting(
                                registryKey,
                                parsed.Key,
                                parsed.ValueName,
                                null,
                                RegistryValueKind.None
                            );
                        }
                    }
                    gpo.Save();
                }
                return 0;
            }
        }

        private delegate object ToObject(string text, RegistryValueKind kind);

        private static object TextToObject(string text, RegistryValueKind kind)
        {
            switch (kind)
            {
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                    return text;
                case RegistryValueKind.MultiString:
                    return text.Split(';');
                case RegistryValueKind.DWord:
                    return text.StartsWith("-") ? (int)Convert.ToUInt32(text) : Convert.ToInt32(text);
                case RegistryValueKind.QWord:
                    return text.StartsWith("-") ? (int)Convert.ToUInt64(text) : Convert.ToInt64(text);
                case RegistryValueKind.Binary:
                    return HexToBytes(text);
                default:
                    return null;
            }
        }

        private static object HexToObject(string text, RegistryValueKind kind)
        {
            return BytesToRegistryValue(HexToBytes(text), kind);
        }

        private static byte[] HexToBytes(string text)
        {
            text = text.Replace("-", "").Trim();
            var bytes = new byte[text.Length / 2];
            for (int x = 0; x < bytes.Length; x++)
            {
                bytes[x] = Convert.ToByte(text.Substring(2 * x, 2), 16);
            }
            return bytes;
        }
    }
}
