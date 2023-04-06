using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibEditLGPO
{
    public class GroupPolicyObjectSettings
    {
        public bool LoadRegistryInformation { get; }
        public bool ReadOnly { get; }

        public GroupPolicyObjectSettings(bool loadRegistryInfo = true, bool readOnly = false)
        {
            LoadRegistryInformation = loadRegistryInfo;
            ReadOnly = readOnly;
        }
    }
}
