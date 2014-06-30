//The type filler construct types for every AST node, without checking type consistency
//--Lucky, 2014 june 30
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace FrontEnd
{
    public class TypeFiller : Visitor
    {
        public TypeFiller(NameSpace toplevelNs) 
        {
            tpNs = toplevelNs;
        }

        public override void Visit(AST_kary n, object data)
        {
            switch (n.Tag)
            {

                default:
                    {
                        BypassKary(n, data); break;
                    }
            }
        }

        public override void Visit(AST_nonleaf n, object data)
        {
            switch (n.Tag)
            {
                case NodeType.Class:
                    {
                        //retrieve the class CbType
                        Debug.Assert(n[0].Tag == NodeType.Ident);
                        AST_leaf classname = (AST_leaf)n[0];
                        string classname_str = classname.Sval;
                        CbClass classRef = (CbClass)tpNs.LookUp(classname_str);
                        Debug.Assert(classRef != null);
                        TravelStatus status = new TravelStatus();
                        status.InClass = classRef;
                        BypassNonleaf(n, status);
                        break;
                    }
                case NodeType.Const:
                    {
                        TravelStatus status = (TravelStatus)data;
                        Debug.Assert((status != null) && (status.InClass != null)); //we must be in a class to see a const decl

                        break;
                    }
                default:
                    {
                        BypassNonleaf(n, data); break;
                    }
            }
        }

        public override void Visit(AST_leaf n, object data)
        {
            base.Visit(n, data);
        }
        /*********************************/
        private NameSpace tpNs;
        /********************************/
        private void BypassKary(AST_kary n, object data)
        {
            for (int i = 0; i < n.NumChildren; ++i)
            {
                if (n[i] != null) n[i].Accept(this, data);
            }
        }

        private void BypassNonleaf(AST_nonleaf n, object data)
        {
            for (int i = 0; i < n.NumChildren; ++i)
            {
                if (n[i] != null) n[i].Accept(this, data);
            }
        }
        
        private class TravelStatus
        {
            public CbClass InClass;
        }
    }


}
