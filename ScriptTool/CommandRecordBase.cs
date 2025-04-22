using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptTool
{
    internal class CommandRecordBase
    {
        public int Code { get; set; }
        public int Addr { get; set; }
        public int Size { get; set; }
    }
}
