using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace CbCompiler
{
	class CbCompilerKernel
    {
        public class StartUpArgs
        {
            public bool ListTokens;
            public bool IsDebug;
            public string SourceFileName;
            public StartUpArgs()
            {
                ListTokens = false;
                IsDebug = false;
            }
        }

        public CbCompilerKernel(CbCompilerKernel.StartUpArgs args_in)
        {
            args = args_in;
        }

        public void Launch()
        {
            FileStream sourcefile = File.OpenRead(args.SourceFileName);
            scanner = new FrontEnd.Scanner(sourcefile);
            parser = new FrontEnd.Parser(scanner);
            parser.Parse();
        }

        /***********************************/
        private StartUpArgs args;
        private FrontEnd.Scanner scanner;
        private FrontEnd.Parser parser;
    };
    
    class CbCompilerEntry
	{
		static void Main(string [] args)
		{
            CbCompilerKernel.StartUpArgs parsedArgs;
            bool argSuccess = ParseCmdArgs(args, out parsedArgs);
            if (argSuccess)
            {
                //Procedure - OO boundary
                CbCompilerKernel kernel = new CbCompilerKernel(parsedArgs);
                kernel.Launch();
            }
		}

        private static bool ParseCmdArgs(string [] args, out CbCompilerKernel.StartUpArgs parsedArgs)
        { 
            parsedArgs = new CbCompilerKernel.StartUpArgs();
            foreach (string arg in args)
            {
                bool argCaught = false;
                if (arg[0] == '-')
                {
                    //a switch command
                    string cmd = arg.Substring(1);
                    if (cmd.CompareTo("tokens") == 0) { parsedArgs.ListTokens = true; argCaught = true; continue; };
                    if (cmd.CompareTo("debug") == 0) { parsedArgs.IsDebug = true; argCaught = true; continue; };
                }
                else
                {
                    //a string parameter
                    parsedArgs.SourceFileName = arg;
                    argCaught = true;
                }
                if (argCaught == false) { Console.WriteLine("Unrecognized parameter:" + arg); return false; };
            }
            return true;//we've go through all args
        }
	}

 
}

