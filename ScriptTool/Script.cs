using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ScriptTool
{
    internal class Script
    {
        private byte[] _script = [];
        private Encoding _encoding = Encoding.UTF8;

        private readonly List<Label> _labels = [];
        private readonly List<Variable> _variables = [];

        private List<CommandRecordBase> _commands = [];
        private string _disassembly = string.Empty;

        public void Load(string filePath, Encoding encoding)
        {
            _script = File.ReadAllBytes(filePath);
            _encoding = encoding;

            Parse();
        }

        private void Parse()
        {
            // |-----------|
            // | Header    |
            // |-----------|
            // | Code      |
            // |-----------|
            // | Label     |
            // |-----------|
            // | Variable  |
            // |-----------|

            var stream = new MemoryStream(_script);
            var reader = new BinaryReader(stream);

            var codePos = reader.ReadInt32();
            var varCount = reader.ReadInt32();
            var labelPos = reader.ReadInt32();
            var varId = reader.ReadInt32();
            var varPos = reader.ReadInt32();

            ReadCode(reader, codePos, labelPos - codePos);

            ReadLabels(reader, labelPos);

            ReadVariables(reader, varPos, varCount, varId);
        }

        private void ParseBlock(BinaryReader reader, int codePos, int codeLength,
            HashSet<int> registeredCommand, StringBuilder dis, bool writeDis, List<CommandRecordBase> commandRecords)
        {
            var codePosEnd = codePos + codeLength;

            reader.BaseStream.Position = codePos;

            while (reader.BaseStream.Position < codePosEnd)
            {
                int addr = Convert.ToInt32(reader.BaseStream.Position);
                int code = reader.ReadUInt16();

                var command = new CommandRecordBase
                {
                    Code = code,
                    Addr = addr,
                    Size = 0,
                };

                switch (code)
                {
                    case 0x0000:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | nop0");
                        break;
                    }
                    case 0x0001:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | nop1");
                        break;
                    }
                    case 0x0002:
                    {
                        var value = reader.ReadInt32();
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | push dword 0x{value:X8}");
                        break;
                    }
                    case 0x0004:
                    {
                        var str = reader.ReadEncryptedString(_encoding).Escape();
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | push str \"{str:X8}\"");
                        break;
                    }
                    case 0x0005:
                    {
                        var count = reader.ReadInt32();
                        var buffer = reader.ReadBytes(count);
                        var str = Convert.ToHexString(buffer);

                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | push bin \"{str}\"");

                        // Analyze binary

                        var savePos = reader.BaseStream.Position;

                        var binStartPos = addr + 6;
                        var binEndPos = binStartPos + count;

                        if (writeDis)
                            dis.AppendLine($"{binStartPos:X8} | ; block start of {addr:X8}");

                        var pushBinCommand = new CommandRecordPushBin
                        {
                            Code = code,
                            Addr = addr,
                            Size = 0,
                        };

                        ParseBlock(reader, binStartPos, count, registeredCommand, dis, writeDis,
                            pushBinCommand.Commands);

                        command = pushBinCommand;

                        if (writeDis)
                            dis.AppendLine($"{binEndPos:X8} | ; block end of {addr:X8}");

                        reader.BaseStream.Position = savePos;

                        break;
                    }
                    case 0x0007:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | jump_indirect");
                        break;
                    }
                    case 0x0008:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | jump_true");
                        break;
                    }
                    case 0x0009:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | jump_false");
                        break;
                    }
                    case 0x000A:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | call");
                        break;
                    }
                    case 0x000B:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | call_true");
                        break;
                    }
                    case 0x000C:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | call_false");
                        break;
                    }
                    case 0x000D:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | ret");
                        break;
                    }
                    case 0x000E:
                    {
                        int id = reader.ReadUInt16();
                        registeredCommand.Add(id);
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | add_cmd 0x{id:X4}");
                        break;
                    }
                    case 0x000F:
                    {
                        int id = reader.ReadUInt16();
                        registeredCommand.Add(id);
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | add_cmd_alias 0x{id:X4}");
                        break;
                    }
                    case 0x0010:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | stog");
                        break;
                    }
                    case 0x0011:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | gtos");
                        break;
                    }
                    case 0x0012:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | jump_break");
                        break;
                    }
                    case 0x0013:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | jump_reset");
                        break;
                    }
                    case 0x0014:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | load_script");
                        break;
                    }
                    case 0x0020:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | cmd_{code:X4}");
                        break;
                    }
                    case 0x0021:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | cmd_{code:X4}");
                        break;
                    }
                    case 0x0022:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | find_label");
                        break;
                    }
                    case 0x0023:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | cmd_{code:X4}");
                        break;
                    }
                    case 0x0024:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | cmd_{code:X4}");
                        break;
                    }
                    case 0x0025:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | exec_cmd");
                        break;
                    }
                    case 0x0040:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | clock");
                        break;
                    }
                    case 0x0041:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | tick");
                        break;
                    }
                    case 0x0042:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | ticks");
                        break;
                    }
                    case 0x0043:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | wait");
                        break;
                    }
                    case 0x0044:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | past");
                        break;
                    }
                    case 0x0064:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | drop");
                        break;
                    }
                    case 0x0065:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | drops");
                        break;
                    }
                    case 0x0066:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | dup");
                        break;
                    }
                    case 0x0067:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | dups");
                        break;
                    }
                    case 0x0068:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | pick");
                        break;
                    }
                    case 0x0069:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | push");
                        break;
                    }
                    case 0x006A:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | depth");
                        break;
                    }
                    case 0x006B:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | system_depth");
                        break;
                    }
                    case 0x006C:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | return_depth");
                        break;
                    }
                    case 0x006D:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | return_addr");
                        break;
                    }
                    case 0x0096:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | loop");
                        break;
                    }
                    case 0x0097:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | loop(internal)");
                        break;
                    }
                    case 0x0098:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | repeat");
                        break;
                    }
                    case 0x0099:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | repeat(internal)");
                        break;
                    }
                    case 0x009A:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | for");
                        break;
                    }
                    case 0x009B:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | for(internal)");
                        break;
                    }
                    case 0x009C:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | while");
                        break;
                    }
                    case 0x009D:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | while(internal)");
                        break;
                    }
                    case 0x009E:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | while(internal)");
                        break;
                    }
                    case 0x009F:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | do");
                        break;
                    }
                    case 0x00A0:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | break");
                        break;
                    }
                    case 0x00A1:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | if_break");
                        break;
                    }
                    case 0x00A2:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | nif_break");
                        break;
                    }
                    case 0x00A3:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | continue");
                        break;
                    }
                    case 0x00A4:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | if_continue");
                        break;
                    }
                    case 0x00A5:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | nif_continue");
                        break;
                    }
                    case 0x00A6:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | call_command");
                        break;
                    }
                    case 0x00A7:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | if_command");
                        break;
                    }
                    case 0x00A8:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | nif_command");
                        break;
                    }
                    case 0x00C8:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | drop2");
                        break;
                    }
                    case 0x00C9:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | drops2");
                        break;
                    }
                    case 0x00CA:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | move12");
                        break;
                    }
                    case 0x00CB:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | moves12");
                        break;
                    }
                    case 0x00CC:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | move21");
                        break;
                    }
                    case 0x00CD:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | moves21");
                        break;
                    }
                    case 0x00CE:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | dup2");
                        break;
                    }
                    case 0x00CF:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | dup12");
                        break;
                    }
                    case 0x00D0:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | dup21");
                        break;
                    }
                    case 0x00D2:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | pick2");
                        break;
                    }
                    case 0x00D3:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | push2");
                        break;
                    }
                    case 0x00D4:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | depth2");
                        break;
                    }
                    case 0x00FA:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | add");
                        break;
                    }
                    case 0x00FB:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | adds");
                        break;
                    }
                    case 0x00FC:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | sub");
                        break;
                    }
                    case 0x00FD:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | subs");
                        break;
                    }
                    case 0x00FE:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | mult");
                        break;
                    }
                    case 0x00FF:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | div");
                        break;
                    }
                    case 0x0100:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | mod");
                        break;
                    }
                    case 0x0101:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | negate");
                        break;
                    }
                    case 0x0102:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | max");
                        break;
                    }
                    case 0x0103:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | maxs");
                        break;
                    }
                    case 0x0104:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | min");
                        break;
                    }
                    case 0x0105:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | mins");
                        break;
                    }
                    case 0x0106:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | random");
                        break;
                    }
                    case 0x0107:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | set_random_seed");
                        break;
                    }
                    case 0x012C:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | not");
                        break;
                    }
                    case 0x012D:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | and");
                        break;
                    }
                    case 0x012E:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | ands");
                        break;
                    }
                    case 0x012F:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | or");
                        break;
                    }
                    case 0x0130:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | ors");
                        break;
                    }
                    case 0x0131:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | xor");
                        break;
                    }
                    case 0x0132:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | xors");
                        break;
                    }
                    case 0x0133:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | equal");
                        break;
                    }
                    case 0x0134:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | different");
                        break;
                    }
                    case 0x0135:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | larger");
                        break;
                    }
                    case 0x0136:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | larger_equal");
                        break;
                    }
                    case 0x0137:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | less");
                        break;
                    }
                    case 0x0138:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | less_equal");
                        break;
                    }
                    case 0x015E:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | strlen");
                        break;
                    }
                    case 0x015F:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | strleft");
                        break;
                    }
                    case 0x0160:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | strright");
                        break;
                    }
                    case 0x0161:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | strmid");
                        break;
                    }
                    case 0x0190:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | fopen");
                        break;
                    }
                    case 0x0191:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | fcreate");
                        break;
                    }
                    case 0x0192:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | fclose");
                        break;
                    }
                    case 0x0193:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | fflush");
                        break;
                    }
                    case 0x0194:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | feof");
                        break;
                    }
                    case 0x0195:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | ferror");
                        break;
                    }
                    case 0x0196:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | flocation");
                        break;
                    }
                    case 0x0197:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | fseek");
                        break;
                    }
                    case 0x0198:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | fseekend");
                        break;
                    }
                    case 0x0199:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | fseekadd");
                        break;
                    }
                    case 0x019A:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | fread1");
                        break;
                    }
                    case 0x019B:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | freadU1");
                        break;
                    }
                    case 0x019C:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | fread2");
                        break;
                    }
                    case 0x019D:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | freadU2");
                        break;
                    }
                    case 0x019E:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | fread4");
                        break;
                    }
                    case 0x019F:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | freadU4");
                        break;
                    }
                    case 0x01A0:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | freadline");
                        break;
                    }
                    case 0x01A1:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | freadstr");
                        break;
                    }
                    case 0x01A2:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | freaddata");
                        break;
                    }
                    case 0x01A3:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | fwrite1");
                        break;
                    }
                    case 0x01A4:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | fwrite2");
                        break;
                    }
                    case 0x01A5:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | fwrite4");
                        break;
                    }
                    case 0x01A6:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | fwritechars");
                        break;
                    }
                    case 0x01A7:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | fwritestr");
                        break;
                    }
                    case 0x01A8:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | fwritedata");
                        break;
                    }
                    case 0x01B8:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | file_delete");
                        break;
                    }
                    case 0x01B9:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | file_copy");
                        break;
                    }
                    case 0x01BA:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | file_move");
                        break;
                    }
                    case 0x01C2:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | b_not");
                        break;
                    }
                    case 0x01C3:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | b_and");
                        break;
                    }
                    case 0x01C4:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | b_or");
                        break;
                    }
                    case 0x01C5:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | b_xor");
                        break;
                    }
                    case 0x01C6:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | b_shift_l");
                        break;
                    }
                    case 0x01C7:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | b_shift_r");
                        break;
                    }
                    case 0x01C8:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | b_shift_ar");
                        break;
                    }
                    case 0x01F4:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | ArrayNew");
                        break;
                    }
                    case 0x01F5:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | ArrayDelete");
                        break;
                    }
                    case 0x01F6:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | ArrayLength");
                        break;
                    }
                    case 0x01F7:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | ArrayResize");
                        break;
                    }
                    case 0x01F8:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | ArrayAppend");
                        break;
                    }
                    case 0x01F9:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | ArraySet");
                        break;
                    }
                    case 0x01FA:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | ArrayGet");
                        break;
                    }
                    case 0x01FB:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | ArraySortInsert");
                        break;
                    }
                    case 0x01FC:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | ArraySortedSearch");
                        break;
                    }
                    case 0x0208:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | FlagsInit");
                        break;
                    }
                    case 0x0209:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | FlagsSet");
                        break;
                    }
                    case 0x020A:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | FlagsReset");
                        break;
                    }
                    case 0x020B:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | FlagsPut");
                        break;
                    }
                    case 0x020C:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | FlagsGet");
                        break;
                    }
                    case 0x020D:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | freadflags");
                        break;
                    }
                    case 0x020E:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | fwriteflags");
                        break;
                    }
                    case 0x020F:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | system_stacks_flush");
                        break;
                    }
                    case 0x0210:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | FlagsCopy");
                        break;
                    }
                    case 0x021C:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | gettime");
                        break;
                    }
                    case 0x021D:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | decodetime");
                        break;
                    }
                    case 0x4000:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | event_timer");
                        break;
                    }
                    case 0x4001:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | event_mouse_move");
                        break;
                    }
                    case 0x4002:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | event_mouse_left");
                        break;
                    }
                    case 0x4003:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | event_mouse_right");
                        break;
                    }
                    case 0x4004:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | event_key_press");
                        break;
                    }
                    case 0x4005:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | event_key_release");
                        break;
                    }
                    case 0x4006:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | show_hook");
                        break;
                    }
                    case 0x4007:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | event_mouse_wheel");
                        break;
                    }
                    case 0x6000:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerNumber");
                        break;
                    }
                    case 0x6001:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerReset");
                        break;
                    }
                    case 0x6002:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerImage");
                        break;
                    }
                    case 0x6003:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerTile");
                        break;
                    }
                    case 0x6004:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerText");
                        break;
                    }
                    case 0x6005:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerGetXY");
                        break;
                    }
                    case 0x6006:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerGetWH");
                        break;
                    }
                    case 0x6007:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerGetImageSize");
                        break;
                    }
                    case 0x6008:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerActive");
                        break;
                    }
                    case 0x6009:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerGetActive");
                        break;
                    }
                    case 0x600A:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerMove");
                        break;
                    }
                    case 0x600B:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerSetWindow");
                        break;
                    }
                    case 0x600C:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerGetWindow");
                        break;
                    }
                    case 0x600D:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerAddText");
                        break;
                    }
                    case 0x600E:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerAddTextN");
                        break;
                    }
                    case 0x600F:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerTextFormat");
                        break;
                    }
                    case 0x6010:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerTextClear");
                        break;
                    }
                    case 0x6011:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerTextFont");
                        break;
                    }
                    case 0x6012:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerTextColor");
                        break;
                    }
                    case 0x6013:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerTextColorN");
                        break;
                    }
                    case 0x6014:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerTextLocate");
                        break;
                    }
                    case 0x6015:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerTextLocateN");
                        break;
                    }
                    case 0x6016:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerTextGetLocate");
                        break;
                    }
                    case 0x6017:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerTextGetID");
                        break;
                    }
                    case 0x6018:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerTextGetRegion");
                        break;
                    }
                    case 0x6019:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerTextChangeColor");
                        break;
                    }
                    case 0x601A:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerTextClearID");
                        break;
                    }
                    case 0x601B:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerSetDensity");
                        break;
                    }
                    case 0x601C:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerGetDensity");
                        break;
                    }
                    case 0x601D:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerSetWindow2");
                        break;
                    }
                    case 0x601E:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerGetWindow2");
                        break;
                    }
                    case 0x601F:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerStretch");
                        break;
                    }
                    case 0x6020:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerGetColorCode");
                        break;
                    }
                    case 0x6021:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerGetScreenMode");
                        break;
                    }
                    case 0x6022:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerSetScreenMode");
                        break;
                    }
                    case 0x6023:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerSetMethod");
                        break;
                    }
                    case 0x6024:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerGetMethod");
                        break;
                    }
                    case 0x6025:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerSetDefaultMethod");
                        break;
                    }
                    case 0x6026:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerGetDefaultMethod");
                        break;
                    }
                    case 0x6027:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerActiveBlind");
                        break;
                    }
                    case 0x6029:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerSetBlindColor");
                        break;
                    }
                    case 0x602B:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerSetOffset");
                        break;
                    }
                    case 0x602D:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerSetTitle");
                        break;
                    }
                    case 0x602E:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerSetAlpha");
                        break;
                    }
                    case 0x6030:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | LayerSurface");
                        break;
                    }
                    case 0x6100:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | SoundIsEnable");
                        break;
                    }
                    case 0x6101:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | SoundRead");
                        break;
                    }
                    case 0x6102:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | SoundDelete");
                        break;
                    }
                    case 0x6103:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | SoundPlay");
                        break;
                    }
                    case 0x6104:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | SoundLoopPlay");
                        break;
                    }
                    case 0x6105:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | SoundStop");
                        break;
                    }
                    case 0x6106:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | SoundStatus");
                        break;
                    }
                    case 0x6107:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | SoundGetLength");
                        break;
                    }
                    case 0x6200:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | CddaPlay");
                        break;
                    }
                    case 0x6201:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | CddaPlayLoop");
                        break;
                    }
                    case 0x6202:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | CddaStop");
                        break;
                    }
                    case 0x6203:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | CddaSetDrive");
                        break;
                    }
                    case 0x6204:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | CddaGetDrive");
                        break;
                    }
                    case 0x6501:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | AviOpenFile");
                        break;
                    }
                    case 0x6508:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | AviClose");
                        break;
                    }
                    case 0x6509:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | AviGetFrameLength");
                        break;
                    }
                    case 0x650A:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | AviGetInfo");
                        break;
                    }
                    case 0x6520:
                    {
                        if (writeDis)
                            dis.AppendLine($"{addr:X8} | AviSendFrameImage");
                        break;
                    }
                    default:
                    {
                        if (registeredCommand.Contains(code))
                        {
                            if (writeDis)
                                dis.AppendLine($"{addr:X8} | exec_cmd 0x{code:X4}");
                            break;
                        }

                        throw new Exception($"Unknow command ID {code:X4} at {addr:X8} .");
                    }
                }

                command.Size = Convert.ToInt32(reader.BaseStream.Position) - addr;

                commandRecords.Add(command);
            }
        }

        private void ReadCode(BinaryReader reader, int codePos, int codeLength)
        {
            var registeredCommand = new HashSet<int>();

            var dis = new StringBuilder(0x1000000);

            _commands = new List<CommandRecordBase>(0x100000);

            ParseBlock(reader, codePos, codeLength, registeredCommand, dis, true, _commands);

            _disassembly = dis.ToString();
        }

        private void ReadLabels(BinaryReader reader, int labelPos)
        {
            reader.BaseStream.Position = labelPos;

            _labels.Clear();
            _labels.EnsureCapacity(0x100000);

            for (var addr = reader.ReadInt32(); addr != -1; addr = reader.ReadInt32())
            {
                var name = reader.ReadEncryptedString(_encoding);

                var label = new Label
                {
                    Addr = addr,
                    Name = name,
                };

                _labels.Add(label);
            }
        }

        private void ReadVariables(BinaryReader reader, int varPos, int varCount, int varId)
        {
            reader.BaseStream.Position = varPos;

            _variables.Clear();
            _variables.EnsureCapacity(0x100000);

            for (var type = reader.ReadByte(); type != 0xFF; type = reader.ReadByte())
            {
                switch (type)
                {
                    case 1:
                    {
                        var val = reader.ReadInt32();

                        var item = new Variable
                        {
                            Id = varId,
                            Value = val
                        };

                        _variables.Add(item);

                        break;
                    }
                    case 2:
                    {
                        var val = reader.ReadEncryptedString(_encoding);

                        var item = new Variable
                        {
                            Id = varId,
                            Value = val
                        };

                        _variables.Add(item);

                        break;
                    }
                    default:
                    {
                        throw new Exception("Unexpected variable type.");
                    }
                }

                varId++;
            }

            if (varId != varCount)
            {
                throw new Exception("Not enough variables were read.");
            }
        }

        private class Label
        {
            public int Addr { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private class Variable
        {
            public int Id { get; set; }
            public object Value { get; set; } = 0;
        }

        public void ExportDisasm(string filePath)
        {
            File.WriteAllText(filePath, _disassembly);
        }

        public void ExportText(string filePath)
        {
            var writer = File.CreateText(filePath);

            var scriptStream = new MemoryStream(_script);
            var scriptReader = new BinaryReader(scriptStream);

            foreach (var cmd in _commands)
            {
                scriptStream.Position = cmd.Addr + 2;

                if (cmd.Code == 0x04)
                {
                    var s = scriptReader.ReadEncryptedString(_encoding);

                    if (string.IsNullOrWhiteSpace(s))
                    {
                        continue;
                    }

                    if (s[0] <= 0x7F)
                    {
                        continue;
                    }

                    s = s.Escape();

                    writer.WriteLine("◇{0:X8}◇{1}", cmd.Addr, s);
                    writer.WriteLine("◆{0:X8}◆{1}", cmd.Addr, s);
                    writer.WriteLine();
                }
            }

            writer.Flush();
        }
    }
}
