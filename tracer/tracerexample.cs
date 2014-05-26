using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CbCompiler;

namespace ConsoleApplication1
{
    class ConsoleListener : TraceListener
    {
        public ConsoleListener(ConsoleColor thiscolor) { color = thiscolor;  }
        public void Write(string msg) { Console.ForegroundColor = color; Console.WriteLine(msg); }
        private ConsoleColor color;
    }
    class Program
    {
        static void Main(string[] args)
        {
            ConsoleListener red = new ConsoleListener(ConsoleColor.Red);
            ConsoleListener blue = new ConsoleListener(ConsoleColor.Blue);
            Tracer.AddTraceListener(red);
            Tracer.AddTraceListener(blue);
            Tracer tracer = new Tracer(Tracer.Component.Lexer, "Test", 0);
            tracer.Write("Lex Error!!!.....");
            tracer.SayTime();
            tracer.StartEvent("For loop 1m times!!");
            for (int i = 0; i < 1000; ++ i)
            {
                for (int j = 0; j < 1000; ++ j)
                {
                    double StupidVar = Math.Sin(2.5);
                }
            }
            tracer.EndEvent("For loop 1m times!!");
            Console.ReadKey();
        }
    }
}
