using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CbCompiler
{
    interface TraceListener
    {
        void Write(string msg);
    }

    class Tracer
    {
        public enum Component { Lexer, Parser };
        public static int AddTraceListener(TraceListener listener)
        {
            lock (threadLock)
            {
                listenerList.Add(listener);
                return listenerList.Count;
            }
        }

        public static void ClearListener()
        {
            lock (threadLock)
            {
                listenerList.Clear();
            }
        }

        public static int GlobalTracingLevel
        {
            get
            {
                int lvl;
                lock (threadLock)
                {
                    lvl = globalLevel;
                }
                return lvl;
            }
            set{
                lock (threadLock)
                {
                    globalLevel = value;
                }
            }
        }

        public Tracer(Component component, string name, int ID)
        {
            //construct the prefix string
            switch (component)
            {
                case Component.Lexer :
                    { ComponentMarker = "Lex"; break; }
                case Component.Parser :
                    { ComponentMarker = "Pas"; break; }
            }
            ComponentName = name;
            ComponentID = ID.ToString();

            Prefix = ComponentMarker + "|" + ComponentName + "." + ComponentID + ":";
            eventMap = new Dictionary<string, EventDescription>();
        }

        public void Write(string msg) { WriteAll(msg, -1); }
        public void Write(string msg, int tracingLevel) { WriteAll(msg, tracingLevel); }
        public void SayTime() { WriteAll(DateTime.Now.ToString(), -1); }
        public void SayTime(int tracingLevel) { WriteAll(DateTime.Now.ToString(), tracingLevel); }
        public void StartEvent(string eventname) { StartEvent(eventname, -1); }
        public void StartEvent(string eventname, int tracingLevel)
        { 
            EventDescription desc = new EventDescription(eventname, DateTime.Now, tracingLevel);
            eventMap.Add(eventname, desc);
            Write("Start Event: " + eventname + ". At time:", tracingLevel);
            SayTime(tracingLevel);
          }
        public bool EndEvent(string eventname)
        {
            EventDescription desc;
            bool foundKey = eventMap.TryGetValue(eventname, out desc);
            if (foundKey == true)
            {
                TimeSpan span = DateTime.Now - desc.startTime;
                Write("End Event: " + eventname + ". At time:", desc.tracingLevel);
                SayTime(desc.tracingLevel);
                Write("Time elapsed: " + span.ToString(), desc.tracingLevel);
                eventMap.Remove(eventname);
                return true;
            }
            else
            { return false; }
        }
        /*-----------------------------------------------------------------*/

        private static int globalLevel = 0;
        private static List<TraceListener> listenerList = new List<TraceListener>();
        private static Object threadLock = new Object();

        /*----------------------------------------------------------------*/

        private string ComponentMarker;
        private string ComponentName;
        private string ComponentID;
        private string Prefix;

        private class EventDescription
        { 
            public string eventName;
            public DateTime startTime;
            public int tracingLevel;
            public EventDescription(string name, DateTime time, int level)
            { eventName = name; startTime = time; tracingLevel = level; }
        }

        private Dictionary<string, EventDescription> eventMap;

        private void WriteAll(string msg, int level)
        {
            lock (threadLock)
            {
                if ((level < 0) || (level >= globalLevel))
                {
                    string decoratedmsg = Prefix + msg;
                    
                    foreach (TraceListener listener in listenerList)
                    { listener.Write(decoratedmsg); }
                }
            }
        }
    }
}
