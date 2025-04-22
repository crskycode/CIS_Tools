using System;
using System.IO;
using System.Text;

namespace ScriptTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("CIS Script Tool");
                Console.WriteLine("  created by Crsky");
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine("  Disassemble : ScriptTool -d -in [script.dat] -icp [shift_jis] -out [output.txt]");
                Console.WriteLine("  Export Text : ScriptTool -e -in [script.dat] -icp [shift_jis] -out [output.txt]");
                Console.WriteLine();
                Console.WriteLine("Press any key to continue...");

                Environment.ExitCode = 1;
                Console.ReadKey();

                return;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var parsedArgs = CommandLineParser.ParseArguments(args);

            // Common arguments
            CommandLineParser.EnsureArguments(parsedArgs, "-in", "-icp", "-out");

            var inputPath = Path.GetFullPath(parsedArgs["-in"]);
            var outputPath = Path.GetFullPath(parsedArgs["-out"]);
            var inputEncoding = Encoding.GetEncoding(parsedArgs["-icp"]);

            // Disassemble
            if (parsedArgs.ContainsKey("-d"))
            {
                var script = new Script();
                script.Load(inputPath, inputEncoding);
                script.ExportDisasm(outputPath);
                return;
            }

            // Export Text
            if (parsedArgs.ContainsKey("-e"))
            {
                var script = new Script();
                script.Load(inputPath, inputEncoding);
                script.ExportText(outputPath);
                return;
            }
        }
    }
}
