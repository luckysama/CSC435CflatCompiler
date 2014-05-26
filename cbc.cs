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
        }

        public CbCompilerKernel(CbCompilerKernel.StartUpArgs args)
        { }

        public void Launch()
        { }
    };
    
    class CbCompilerEntry
	{
		static void Main(string [] args)
		{
            CbCompilerKernel.StartUpArgs parsedArgs;
            ParseCmdArgs(args, out parsedArgs);
            CbCompilerKernel kernel = new CbCompilerKernel(parsedArgs);
            kernel.Launch();
		}

        private static void ParseCmdArgs(string [] args, out CbCompilerKernel.StartUpArgs parsedArgs)
        { }
	}

 
}

