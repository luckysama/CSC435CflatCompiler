using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace FrontEnd
{
    public class TypeVisitor : Visitor
    {
        private TextWriter f;
        private int indent = 0;
        
        private class TravelStatus
        {
            public CbClass InClass = null;
            public CbType InField = null;
            public CbType InConst = null;
            public CbMethod InMethod = null;
            public bool RequestReturn = false;
            public string Ident = null;
            public CbType basicType = null;
            public bool IsArray = false;
            public enum MethodAttrTag { None, Static, Virtual, Override };
            public MethodAttrTag methodAttrs;
        }

        public TypeVisitor(TextWriter output)
        { f = output; indent = 0; }

        public TypeVisitor()
        { f = Console.Out; indent = 0; }

        private string indentString(int indent)
        { return " ".PadRight(2 * indent); }

        public override void Visit(AST_kary n, object data)
        {
            switch (n.Tag)
            {
                case NodeType.UsingList:
                    {
                        //All of using list children should be identifiers
                        //AddAllSystemTokens();
                        break;
                    }
                case NodeType.IdList:
                    {
                        TravelStatus status = (TravelStatus)(data);
                        if ((status.RequestReturn == true) && (status.InField != null))
                        //we're in a field decl, push in all declared names
                        {
                            for (int i = 0; i < n.NumChildren; ++i)
                            {
                                n[i].Accept(this, data);
                                CbField field = new CbField(status.Ident, status.InField);
                                CheckSymbolAdd(status.InClass.AddMember(field), n.LineNumber, status.Ident);
                            }
                        }
                        else BypassKary(n, data);
                        break;
                    }
                case NodeType.Formal:
                    {
                        TravelStatus status = (TravelStatus)(data);
                        if ((status.RequestReturn == true) && (status.InMethod != null))
                        {
                            n[0].Accept(this, data);
                            CbType ParType = DetermineType(status);
                            status.InMethod.ArgType.Add(ParType);
                        }
                        else BypassKary(n, data);
                        break;
                    }
                default:
                    {
                        BypassKary(n, data);
                        break;
                    }
            }            
        }

        public override void Visit(AST_leaf n, object data)
        {
            if (data != null)
            {
                TravelStatus status = (TravelStatus)(data);
                if (status.RequestReturn == true)
                {
                    switch (n.Tag)
                    {
                        case NodeType.Ident: { status.Ident = n.Sval; break; }
                        case NodeType.IntType: { status.Ident = null; status.basicType = CbType.Int; break; }
                        case NodeType.CharType: { status.Ident = null; status.basicType = CbType.Char; break; }
                        case NodeType.StringType: { status.Ident = null; status.basicType = CbType.String; break; }
                        case NodeType.VoidType: { status.Ident = null; status.basicType = CbType.Void; break; }
                        case NodeType.Static: { status.methodAttrs = TravelStatus.MethodAttrTag.Static; break; }
                        case NodeType.Override: { status.methodAttrs = TravelStatus.MethodAttrTag.Override; break; }
                        case NodeType.Virtual: { status.methodAttrs = TravelStatus.MethodAttrTag.Virtual; break; }
                    }
                }
            }
        }

        public override void Visit(AST_nonleaf n, object data)
        {
            switch (n.Tag)
            {
                case NodeType.Class:
                    {
                        //We care about location 0 and 1
                        TravelStatus status = new TravelStatus();
                        status.RequestReturn = true;
                        String classname;
                        CbClass basecls = null;
                        n[0].Accept(this, status);
                        classname = status.Ident;
                        if (n[1] != null)
                        {
                            n[1].Accept(this, status);
                            basecls = (CbClass)NameSpace.TopLevelNames.LookUp(status.Ident);
                        }
                        CbClass cls = new CbClass(classname, basecls);
                        status.InClass = cls;
                        status.RequestReturn = false;
                        status.InField = null;
                        CheckSymbolAdd(NameSpace.TopLevelNames.AddMember(cls), n.LineNumber, cls.Name);
                        if (n[2] != null) n[2].Accept(this, status);
                        break;
                    }
                case NodeType.Field:
                    {
                        TravelStatus status = (TravelStatus)(data);
                        //retrieve the type
                        status.RequestReturn = true;
                        n[0].Accept(this, status);
                        CbType fieldType = DetermineType(status);
                        status.InField = fieldType;
                        //go to ident list to fill in members
                        n[1].Accept(this, status);
                        status.RequestReturn = false;
                        status.InField = null;
                        break;
                    }
                case NodeType.Array:
                    {
                        TravelStatus status = (TravelStatus)(data);
                        if (status.RequestReturn == true)
                            status.IsArray = true;
                        n[0].Accept(this, data);
                        break;
                    }
                case NodeType.Const:
                    {
                        TravelStatus status = (TravelStatus)(data);
                        //retrieve the type
                        status.RequestReturn = true;
                        n[0].Accept(this, status);
                        CbType fieldType = DetermineType(status);
                        status.InConst = fieldType;
                        //retrieve the identifier
                        n[1].Accept(this, status);
                        CbConst cons = new CbConst(status.Ident, fieldType);
                        CheckSymbolAdd(status.InClass.AddMember(cons), n.LineNumber, status.Ident);
                        status.RequestReturn = false;
                        status.InConst = null;
                        break;
                    }
                case NodeType.Method:
                    {
                        TravelStatus status = (TravelStatus)(data);
                        //retrieve the method attr
                        status.RequestReturn = true;
                        if (n[4] != null)  n[4].Accept(this, data);
                        //retrieve the method rt type
                        n[1].Accept(this, data);
                        CbType rtType = DetermineType(status);
                        //retrieve the method name
                        n[1].Accept(this, data);
                        string methodname = status.Ident;
                        //Create the method and dispatch to the formal list
                        CbMethod method = new CbMethod(methodname,
                            status.methodAttrs == TravelStatus.MethodAttrTag.Static,
                            rtType,
                            new List<CbType>());
                        status.InMethod = method;
                        if (n[3] != null) n[3].Accept(this, data);
                        CheckSymbolAdd(status.InClass.AddMember(method), n.LineNumber, method.Name);
                        status.RequestReturn = false;
                        n[4].Accept(this, data);
                        status.InMethod = null;
                        break;
                    }
                default:
                    {
                        BypassNonleaf(n, data);
                        break;
                    }

            }
        }

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

        private CbType DetermineType(TravelStatus status)
        {
            CbType result = null;
            if (status.Ident == null)
            {
                result = status.basicType;
            }
            else
            {
                result = (CbType)NameSpace.TopLevelNames.LookUp(status.Ident);
                status.Ident = null;
            }
            if (status.IsArray == true)
            {
                CbType ArrayWarp = new CFArray(result);
                result = ArrayWarp;
                status.IsArray = false;
            }
            return result;
        }

        private void CheckSymbolAdd(bool addResult, int linenumber, string symbolname)
        {
            if (addResult == false)
            {
                f.WriteLine("Symbol name conflict. Name:{0}, Line:{1}", symbolname, linenumber);
            }
        }

        private void AddAllSystemTokens()
        {
            CbMethod method = null;
            IList<CbType> parlst = null;
            {
                //the basic class
                CbClass StringClass = CbType.String;
                parlst = new List<CbType>();
                parlst.Add(CbType.Int);
                method = new CbMethod("Substring", false, CbType.String, parlst);
                StringClass.AddMember(method);

                method = new CbMethod("Length", false, CbType.Int, null);
                StringClass.AddMember(method);

                NameSpace.TopLevelNames.AddMember(StringClass);
                NameSpace.TopLevelNames.AddMember(CbType.Object);
            }

            {
                //the console class
                CbClass cls = new CbClass("Console", null);
                
                parlst = new List<CbType>();
                parlst.Add(CbType.String);
                method = new CbMethod("Write", true, CbType.Void, parlst);
                cls.AddMember(method);
                
                parlst = new List<CbType>();
                parlst.Add(CbType.String);
                method = new CbMethod("WriteLine", true, CbType.Void, parlst);
                cls.AddMember(method);

                method = new CbMethod("ReadLine", true, CbType.String, null);

                NameSpace.TopLevelNames.AddMember(cls);
            }            
        }
            
    }
}