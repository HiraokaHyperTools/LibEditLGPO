using LibEditLGPO.Helpers;
using LibEditLGPO.PInvoke;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;

namespace LibEditLGPO
{
    public class ComputerGroupPolicyObject : IDisposable
    {
        private bool _disposedValue;
        private readonly ComObj<IGroupPolicyObject> _gpo;

        /// <summary>
        /// The snap-in that processes .pol files
        /// </summary>
        private static readonly Guid RegistryExtension = new Guid(0x35378EAC, 0x683F, 0x11D2, 0xA8, 0x9A, 0x00, 0xC0, 0x4F, 0xBB, 0xCF, 0xA2);

        private static readonly Guid LocalGuid = Guid.Parse("368ece78-a0bf-4884-a77a-68799814783d");

        /// <summary>
        /// True if opened by `OpenLocalMachineGPO`, otherwise False
        /// </summary>
        public bool IsLocal { get; }

        private ComputerGroupPolicyObject()
        {
            _gpo = new ComObj<IGroupPolicyObject>((IGroupPolicyObject)new GPClass());
        }

        /// <summary>
        /// Open by `OpenLocalMachineGPO`
        /// </summary>
        /// <param name="options"></param>
        /// <exception cref="Exception"></exception>
        public ComputerGroupPolicyObject(GroupPolicyObjectSettings options = null) : this()
        {
            var result = _gpo.Value.OpenLocalMachineGPO(GetFlags(options));
            if (result != 0)
            {
                throw new Exception("Unable to open local machine GPO", new Win32Exception((int)result));
            }
            IsLocal = true;
        }

        /// <summary>
        /// Open by `OpenRemoteMachineGPO`
        /// </summary>
        /// <param name="computerName"></param>
        /// <param name="options"></param>
        /// <exception cref="Exception"></exception>
        public ComputerGroupPolicyObject(string computerName, GroupPolicyObjectSettings options = null) : this()
        {
            var result = _gpo.Value.OpenRemoteMachineGPO(computerName, GetFlags(options));
            if (result != 0)
            {
                throw new Exception(string.Format("Unable to open GPO on remote machine '{0}'", computerName), new Win32Exception((int)result));
            }
            IsLocal = false;
        }

        private uint GetFlags(GroupPolicyObjectSettings options)
        {
            uint RegistryFlag = 0x00000001;
            uint ReadonlyFlag = 0x00000002;

            uint flag = 0x00000000;

            if (options?.LoadRegistryInformation ?? false)
            {
                flag |= RegistryFlag;
            }

            if (options?.ReadOnly ?? false)
            {
                flag |= ReadonlyFlag;
            }

            return flag;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _gpo.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Open registry key
        /// </summary>
        /// <param name="section"></param>
        /// <returns>Win32 registry key</returns>
        /// <exception cref="Exception"></exception>
        public RegistryKey GetRootRegistryKey(GroupPolicySection section)
        {
            IntPtr key;
            var result = _gpo.Value.GetRegistryKey((uint)section, out key);
            if (result != 0)
            {
                throw new Exception(string.Format("Unable to get section '{0}'", section), new Win32Exception((int)result));
            }

            var handle = new SafeRegistryHandle(key, true);
            return RegistryKey.FromHandle(handle, RegistryView.Default);
        }

        /// <summary>
        /// Save changed
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Save()
        {
            var result = _gpo.Value.Save(true, true, RegistryExtension, LocalGuid);
            if (result != 0)
            {
                throw new Exception("Error saving machine settings", new Win32Exception((int)result));
            }

            result = _gpo.Value.Save(false, true, RegistryExtension, LocalGuid);
            if (result != 0)
            {
                throw new Exception("Error saving user settings", new Win32Exception((int)result));
            }
        }
    }
}
