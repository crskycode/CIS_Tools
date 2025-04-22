using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptTool
{
    internal class CommandRecordPushBin : CommandRecordBase
    {
        public List<CommandRecordBase> Commands { get; set; } = [];
    }
}
