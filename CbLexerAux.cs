using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;

using CbCompiler;

namespace CbCompiler.FrontEnd
{
    public class ScannerParameters
    {
        public bool WriteToken;
    }

    public sealed partial class Scanner
    {
        public Scanner(ScannerParameters param_in, FileStream stream)
        {
            param = param_in;
            tracer = new Tracer(Tracer.Component.Lexer, "GPLex", 0);
            if (param.WriteToken == true)
            {
                tracer.Write("Print tokens set to true.");
            }
            SetSource(stream);
        }

        public override void yyerror(string msg, params object[] args)
        {
            yyerror(lineNum, msg, args);
        }

        public void yyerror(int lineNum_in, string msg, params object[] args)
        {

            string tracemsg = String.Format("YYerr:At line {0}: ", lineNum_in);
            tracer.Write(tracemsg);
            if (args == null || args.Length == 0)
            {
                tracer.Write(msg);
            }
            else
            {
                tracemsg = String.Format(msg, args);
                tracer.Write(tracemsg);
            }
        }

        /****************************************************************/
        private ScannerParameters param;
        private Tracer tracer;
        private void SayToken(Tokens token)
        {
            if (param.WriteToken == true)
            {
                tracer.Write(token.ToString());
            }
        }

        private void SayToken(Tokens token, string ExtraText)
        {
            if (param.WriteToken == true)
            {
                string msg = token.ToString() + " : " + ExtraText;
                tracer.Write(msg);
            }
        }

        private void SayToken(Tokens token, char ExtraText)
        {
            if (param.WriteToken == true)
            {
                string msg = token.ToString() + " : " + ExtraText;
                tracer.Write(msg);
            }
        }
    }
}