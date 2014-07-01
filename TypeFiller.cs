//The type filler construct types for every class level
//because we don't go into method body, no need for symbol table
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
            TravelStatus status = (TravelStatus)data;
            CbClass ClassContext = null;
            if (status != null)
                ClassContext = status.InClass;
            switch (n.Tag)
            {
                case NodeType.Class:
                    {
                        //retrieve the class CbType                      
                        TravelStatus newstatus = new TravelStatus();
                        newstatus.InClass = (CbClass)n.Type;
                        Debug.Assert(newstatus.InClass != null);
                        BypassNonleaf(n, newstatus);
                        break;
                    }
                case NodeType.Const:
                    {
                        Debug.Assert(ClassContext != null); //we must be in a class to see a const decl
                        //retrieve the type identifier and look up
                        CbType thistype = ParseCompositeType(n[0]);
                        //fill in the type info both on AST and on the class desc
                        n.Type = thistype;
                        AST_leaf cid = (AST_leaf)(n[1]);
                        string cid_str = cid.Sval;
                        CbConst thisConst = (CbConst)ClassContext.Members[cid_str];
                        Debug.Assert(thisConst != null);
                        thisConst.Type = thistype;
                        break;
                    }
                case NodeType.Field:
                    {
                        Debug.Assert(ClassContext != null);
                        CbType thistype = ParseCompositeType(n[0]);
                        AST_kary fields = (AST_kary)(n[1]);
                        for (int i = 0; i < fields.NumChildren; ++i)
                        {
                            AST_leaf id = fields[i] as AST_leaf;
                            string id_str = id.Sval;
                            CbField fieldthis = ClassContext.Members[id_str] as CbField;
                            fieldthis.Type = thistype;
                        }
                            break;
                    }
                case NodeType.Method:
                    {
                        CbType returnType = CbType.Void;
                        if (n[0] != null)
                        {
                            returnType = ParseCompositeType(n[0]);
                        }
                        //Get the identifier
                        AST_leaf mid = (AST_leaf)(n[1]);
                        CbMethod methodthis = ClassContext.Members[mid.Sval] as CbMethod;
                        Debug.Assert(methodthis != null);
                        methodthis.ResultType = returnType;
                        //Parse the parameter list
                        status.InMethod = methodthis;
                        BypassNonleaf(n, status);
                        status.InMethod = null;
                        break;
                    }
                case NodeType.Formal:
                    {
                        Debug.Assert(status.InMethod != null);
                        CbType type = ParseCompositeType(n[0]);
                        status.InMethod.ArgType.Add(type);
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
            //base.Visit(n, data);//noooo don't do this!!!
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

        private CbType ParseCompositeType(AST ast)
        {
            if (ast.Tag == NodeType.Array)
            {
                CbType innerType = ParseCompositeType(ast[0]);
                CFArray arrayWarp = new CFArray(innerType);
                return arrayWarp;
            } else
            {
                switch (ast.Tag)
                {
                    case NodeType.IntType: return CbType.Int;
                    case NodeType.StringType: return CbType.String;
                    case NodeType.CharType: return CbType.Char;
                    case NodeType.VoidType: return CbType.Void;
                    case NodeType.Ident:
                        {
                            AST_leaf identifier = ast as AST_leaf;
                            CbType typethis = tpNs.LookUp(identifier.Sval) as CbType;
                            Debug.Assert(typethis != null);
                            return typethis;
                        }
                    default:
                        {
                            throw new Exception("Unexcepted tag in type parsing.");
                        }
                }
            }
        }
        
        private class TravelStatus
        {
            public CbClass InClass;
            public CbMethod InMethod;
        }
    }


}
