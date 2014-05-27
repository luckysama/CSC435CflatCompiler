using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        
        }

        /***********************************/
        private StartUpArgs args;
    };
    
    class CbCompilerEntry
	{
		static void Main(string [] args)
		{
            CbCompilerKernel.StartUpArgs parsedArgs;
            ParseCmdArgs(args, out parsedArgs);
            //Procedure - OO boundary
            CbCompilerKernel kernel = new CbCompilerKernel(parsedArgs);
            kernel.Launch();
		}

        private static void ParseCmdArgs(string [] args, out CbCompilerKernel.StartUpArgs parsedArgs)
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
                if (argCaught == false) { Console.WriteLine("Unrecognized parameter:" + arg); };
            }
        }
	}

 
}

