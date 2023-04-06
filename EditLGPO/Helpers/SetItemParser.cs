using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EditLGPO.Helpers
{
    internal class ApplyItemParser
    {
        public ApplyItemParser(string line)
        {
            var cells = line.Split(new char[] { ';' }, 5);
            Key = cells[0];
            ValueName = cells[1];
            var kindCells = cells[2].Split(':');
            Kind = (kindCells[0].Length == 0) ? new int?() : int.Parse(kindCells[0]);
            HasTextFilter = kindCells.Contains("text");
            Data = (cells[4]);
        }

        public string Key { get; }
        public string ValueName { get; }
        public int? Kind { get; }
        public bool HasTextFilter { get; }
        public string Data { get; }
    }
}
