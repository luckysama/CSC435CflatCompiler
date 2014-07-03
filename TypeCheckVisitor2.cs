/*
    We now visit and type-check all the parts of the AST which were not
    checked in the first stage of full type-checking.
    
    Author: Nigel Horspool
    
    Dates: 2012-2014
*/

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace FrontEnd {


public class TypeCheckVisitor2: Visitor {
    NameSpace ns;  // namespace for all top-level names and names opened with 'using' clauses
    CbClass currentClass;  // current class being checked (null if there isn't one)
    CbMethod currentMethod;  // current method being checked (null if there isn't one)
    SymTab sy;   // one instance of SymTab used for all method body checking
    int loopNesting;  // current depth of nesting of while loops

    // constructor
    public TypeCheckVisitor2( ) {
        ns = NameSpace.TopLevelNames;  // get the top-level namespace
        currentMethod = null;
        sy = new SymTab();
        loopNesting = 0;
    }
    
		
	public void complain(int line_number, string message) {
    	System.Console.WriteLine("Error["+line_number+"]: "+message);
  	}
    // Note: the data parameter for the Visit methods is never used
    // It is always null (or whatever is passed on the initial call)

	public override void Visit(AST_kary node, object data) {
        switch(node.Tag) {
        case NodeType.ClassList:
            // visit each class declared in the program
            for(int i=0; i<node.NumChildren; i++) {
                node[i].Accept(this, data);
            }
            break;
        case NodeType.MemberList:
            // visit each member of the current class
            for(int i=0; i<node.NumChildren; i++) {
                node[i].Accept(this, data);
            }
            break;
        case NodeType.Block:
            sy.Enter();
            // visit each statement or local declaration
            for(int i=0; i<node.NumChildren; i++) {
                node[i].Accept(this, data);
            }
            sy.Exit();
            break;
        case NodeType.ActualList:
            for(int i=0; i<node.NumChildren; i++) {
                node[i].Accept(this, data);
            }
            break;
        }
    }

	public override void Visit( AST_nonleaf node, object data ) {
        switch(node.Tag) {
        case NodeType.Program:
            node[1].Accept(this, data);  // visit class declarations
            break;
        case NodeType.Class:
            AST_leaf classNameId = node[0] as AST_leaf;
            string className = classNameId.Sval;
            currentClass = ns.LookUp(className) as CbClass;
            Debug.Assert(currentClass != null);
            performParentCheck(currentClass,node.LineNumber);  // check Object is ultimate ancestor
            // now check the class's members
            AST_kary memberList = node[2] as AST_kary;
            for(int i=0; i<memberList.NumChildren; i++) {
                memberList[i].Accept(this,data);
            }
            currentClass = null;
            break;
        case NodeType.Const:
            node[2].Accept(this,data);  // get type of value
            if (!isAssignmentCompatible(node[0].Type,node[2].Type))
                Start.SemanticError(node.LineNumber, "invalid initialization for const");
            break;
        case NodeType.Field:
            break;
        case NodeType.Method:   
            // get the method's type description
            string methname = ((AST_leaf)(node[1])).Sval;
            currentMethod = currentClass.Members[methname] as CbMethod;
            sy.Empty();
            // add each formal parameter to the symbol table
            AST_kary formals = (AST_kary)node[2];
            for(int i=0; i<formals.NumChildren; i++) {
                AST_nonleaf formal = (AST_nonleaf)formals[i];
                string name = ((AST_leaf)formal[1]).Sval;
                SymTabEntry newBinding = sy.Binding(name, formal[1].LineNumber);
                newBinding.Type = formal[0].Type;
            }
            sy.Enter();
            // now type-check the method body
            node[3].Accept(this,data);
            // finally check that static/virtual/override are used correctly
            checkOverride(node);
            currentMethod = null;
            break;
        case NodeType.LocalDecl:
            node[0].Accept(this,data);  // get type for the locals
            AST_kary locals = node[1] as AST_kary;
            for(int i=0; i<locals.NumChildren; i++) {
                AST_leaf local = locals[i] as AST_leaf;
                string name = local.Sval;
                SymTabEntry en = sy.Binding(name, local.LineNumber);
                en.Type = node[0].Type;
            }
            break;
        case NodeType.Assign:
            node[0].Accept(this,data);
            node[1].Accept(this,data);
            if (node[0].Kind != CbKind.Variable)
                Start.SemanticError(node.LineNumber, "target of assignment is not a variable");
            if (!isAssignmentCompatible(node[0].Type, node[1].Type))
                Start.SemanticError(node.LineNumber, "invalid types in assignment statement");
            break;
        case NodeType.If:
            node[0].Accept(this,data);
		CbType nodeType_If = node[0].Type;
		if (nodeType_If != CbType.Int && nodeType_If != CbType.Error)
			Start.SemanticError(node.LineNumber,"If statement conditional must evaluate to type int not {0}",nodeType_If.ToString());
		/* TODO ... check type */
            node[1].Accept(this,data);
            node[2].Accept(this,data);
            break;
        case NodeType.While:
            node[0].Accept(this,data);
		CbType nodeType_While = node[0].Type;
		if (nodeType_While != CbType.Int && nodeType_While != CbType.Error)
			Start.SemanticError(node.LineNumber,"While statement condition must evaluate to type int not {0}",nodeType_While.ToString());
            /* TODO ... check type */
            loopNesting++;
            node[1].Accept(this,data);
            loopNesting--;
            break;
        case NodeType.Return:
            if (node[0] == null) {
                if (currentMethod.ResultType != CbType.Void)
                    Start.SemanticError(node.LineNumber, "missing return value for method");
                break;
            }
            node[0].Accept(this,data);
			//TODO: Incompatible types, use switch statement?
			if (node[0].Type != currentMethod.ResultType && node[0].Type != CbType.Error)
				Start.SemanticError(node.LineNumber, String.Format("expected return type of {0} but got {1}",currentMethod.ResultType.ToString(),node[0].Type.ToString()));	
            /* TODO ... check type of method result */
            break;
        case NodeType.Call:
            node[0].Accept(this,data); // method name (could be a dotted expression)
            node[1].Accept(this,data); // actual parameters
                    /* TODO ... check types */
            //lucky: this thing is too hard. write in a separate method
            checkCall(node);		
            break;
        case NodeType.Dot:
            node[0].Accept(this,data);
            string rhs = ((AST_leaf)node[1]).Sval;
            CbClass lhstype = node[0].Type as CbClass;
            if (lhstype != null) {
                // rhs needs to be a member of the lhs class
                CbMember mem;
                if (lhstype.Members.TryGetValue(rhs,out mem)) {
                    node.Type = mem.Type;
                    if (mem is CbField)
                        node.Kind = CbKind.Variable;
                    if (node[0].Kind == CbKind.ClassName) {
                        // mem has to be a static member
                        if (!mem.IsStatic)
                            Start.SemanticError(node[1].LineNumber, "static member required");
                    } else {
                        // mem has to be an instance member
                        if (mem.IsStatic)
                            Start.SemanticError(node[1].LineNumber,
                                "member cannot be accessed via a reference, use classname instead");
                    }
                } else {
                    Start.SemanticError(node[1].LineNumber,
                        "member {0} not found in class {1}", rhs, lhstype.Name);
                    node.Type = CbType.Error;
                }
                break;
            }
            if (rhs == "Length") {
                // lhs has to be an array or a string
                if (node[0].Type != CbType.String && !(node[0].Type is CFArray))
                    Start.SemanticError(node[1].LineNumber, "member Length not found");
                node.Type = CbType.Int;
                break;
            }
            CbNameSpaceContext lhsns = node[0].Type as CbNameSpaceContext;
            if (lhsns != null) {
                lhstype = lhsns.Space.LookUp(rhs) as CbClass;
                if (lhstype != null) {
                    node.Type = lhstype;
                    node.Kind = CbKind.ClassName;
                    break;
                }
            }
            Start.SemanticError(node[1].LineNumber, "member {0} does not exist", rhs);
            node.Type = CbType.Error;
            break;
        case NodeType.Cast:
            checkTypeSyntax(node[0]);
            node[0].Accept(this,data);
            node[1].Accept(this,data);
            if (!isCastable(node[0].Type, node[1].Type))
                Start.SemanticError(node[1].LineNumber, "invalid cast");
            node.Type = node[0].Type;
            break;
        case NodeType.NewArray:
            node[0].Accept(this,data);
            node[1].Accept(this,data);
            ///////////////////////////
            if (node[0].Type == null)
            {
                Start.SemanticError(node[0].LineNumber, "Unrecognized array type.");
                break;
            }
            if (node[1].Type != CbType.Int && node[1].Type != CbType.Error)
                Start.SemanticError(node[1].LineNumber, "new expression must have int as array size.");
            node.Type = CbType.Array(node[0].Type);
            break;
        case NodeType.NewClass:
            node[0].Accept(this,data);
            /* TODO ... check that operand is a class */
            if (!(node[0].Type is CbClass))
                Start.SemanticError(node[0].LineNumber, "The specified type in new expression is not a class.");
            node.Type = node[0].Type;
            break;
        case NodeType.PlusPlus:
        case NodeType.MinusMinus:
            node[0].Accept(this,data);
            /* TODO ... check types and operand must be a variable */
            if (node[0].Kind != CbKind.Variable)
            {
                Start.SemanticError(node[0].LineNumber, "The identifier in the expression is not a variable (plusplus minusminus).");
            } else if (node[0].Type != CbType.Int && node[0].Type != CbType.Error)
            {
                Start.SemanticError(node[0].LineNumber, "The variable is not of int type (for plusplus and minusminus operator).");
            }
            node.Type = CbType.Int;
            break;
        case NodeType.UnaryPlus:
        case NodeType.UnaryMinus:
            node[0].Accept(this,data);
            /* TODO ... check types */
            if (node[0].Kind != CbKind.Variable)
            {
                Start.SemanticError(node[0].LineNumber, "The identifier in the expression is not a variable (unary operator).");
            }
            else if (node[0].Type != CbType.Int && node[0].Type != CbType.Error)
            {
                Start.SemanticError(node[0].LineNumber, "The variable is not of int type (for unary operator).");
            }
            node.Type = CbType.Int;
            break;
        case NodeType.Index:
            node[0].Accept(this,data);
            node[1].Accept(this,data);
            /* TODO ... check types */
            if (node[0].Kind != CbKind.Variable)
            {
                Start.SemanticError(node[0].LineNumber, "The identifier in the expression is not a variable (Index).");
            } else if (!(node[0].Type is CFArray) || (node[0].Type == CbType.String))
            {
                if (!(node[0].Type == CbType.Error))
                {
                    Start.SemanticError(node[0].LineNumber, "The variable being indexed is not of array or string type (index).");
                    //What's being indexed? we have no idea, must propgate this error
                    node.Type = CbType.Error;
                    break;
                }
            }
             //lucky:At this point, we can be sure about the whole node's type
             //even if the index number expression goes wild.
            if (node[0].Type == CbType.String)
                node.Type = CbType.Char;
            else node.Type = (node[0].Type as CFArray).ElementType;  // FIX THIS
            if (node[1].Type != CbType.Int && node[1].Type != CbType.Error)
            {
                Start.SemanticError(node[0].LineNumber, "The index subscript must be of int type.");
            }
            break;
        case NodeType.Add:
        case NodeType.Sub:
        case NodeType.Mul:
        case NodeType.Div:
        case NodeType.Mod:
            node[0].Accept(this,data);
            node[1].Accept(this,data);
            /* TODO ... check types */
            //lucky: we really can only do int with these, 
            //because Cb has no float
            
            if (node[0].Type != CbType.Int || node[1].Type != CbType.Int)
            {
                if (!(node[0].Type == CbType.Error || node[1].Type == CbType.Error))
                {
                    Start.SemanticError(node[0].LineNumber, "Arithematics can only be done on int types.");
                }                
            } 
            node.Type = CbType.Int;  // FIX THIS
            break;
        case NodeType.Equals:
        case NodeType.NotEquals:
            node[0].Accept(this,data);
            node[1].Accept(this,data);
            node.Type = CbType.Bool;           
            /* TODO ... check types */
            if (node[0].Type != node[1].Type)
            {
                if (!(node[0].Type == CbType.Error || node[1].Type == CbType.Error))
                {
                    Start.SemanticError(node[0].LineNumber, "The two operands of                  the comparsion is of different type (equality).");
                }
            }
            break;
        case NodeType.LessThan:
        case NodeType.GreaterThan:
        case NodeType.LessOrEqual:
        case NodeType.GreaterOrEqual:
            node[0].Accept(this,data);
            node[1].Accept(this,data);        
            node.Type = CbType.Bool;
            /* TODO ... check types */
            if (node[0].Type != CbType.Int || node[1].Type != CbType.Int)
            {
                if (!(node[0].Type == CbType.Error || node[1].Type == CbType.Error))
                {
                    Start.SemanticError(node[0].LineNumber, "Only integer could be compared (in-equality).");
                }
            }
            break;
        case NodeType.And:
        case NodeType.Or:
            node[0].Accept(this,data);
            node[1].Accept(this,data);
            /* TODO ... check types */
            if ((node[0].Type != CbType.Bool) || (node[1].Type != CbType.Bool))
            {
                if (!(node[0].Type == CbType.Error || node[1].Type == CbType.Error))
                {
                    Start.SemanticError(node[0].LineNumber, "The operands for a boolean operation must also be of boolean type (and or).");
                }
            }
            node.Type = CbType.Bool;
            break;
        default:
            throw new Exception("Unexpected tag: "+node.Tag);  
        }
    }

	public override void Visit(AST_leaf node, object data) {
        switch(node.Tag) {
        case NodeType.Ident:
            string name = node.Sval;
            SymTabEntry local = sy.LookUp(name);
            if (local != null) {
                node.Type = local.Type;
                node.Kind = CbKind.Variable;
                return;
            }
            CbMember mem;
            if (currentClass.Members.TryGetValue(name,out mem)) {
                node.Type = mem.Type;
                if (mem is CbField)
                    node.Kind = CbKind.Variable;
                break;
            }
            CbClass t = ns.LookUp(name) as CbClass;
            if (t != null) {
                node.Type = t;
                node.Kind = CbKind.ClassName;
                break;
            }
            NameSpace lhsns = ns.LookUp(name) as NameSpace;
            if (lhsns != null) {
                node.Type = new CbNameSpaceContext(lhsns);
                break;
            }
            node.Type = CbType.Error;;
            Start.SemanticError(node.LineNumber, "{0} is unknown", name);
            break;
        case NodeType.Break:
            if (loopNesting <= 0)
                Start.SemanticError(node.LineNumber, "break can only be used inside a loop");
            break;
        case NodeType.Null:
            node.Type = CbType.Null;
            break;
        case NodeType.IntConst:
            node.Type = CbType.Int;
            break;
        case NodeType.StringConst:
            node.Type = CbType.String;
            break;
        case NodeType.CharConst:
            node.Type = CbType.Char;
            break;
        case NodeType.Empty:
            break;
        case NodeType.IntType:
            node.Type = CbType.Int;
            break;
        case NodeType.CharType:
            node.Type = CbType.Char;
            break;
        case NodeType.StringType:
            node.Type = CbType.String;
            break;
        default:
            throw new Exception("Unexpected tag: "+node.Tag);
        }
    }

    private void performParentCheck(CbClass c, int lineNumber) {
        /* TODO
           code to check that c's ultimate ancestor is Object.
           Be careful not to get stuck if the parent relationship
           contains a cycle.
           The lineNumber parameter is used in error messages.
        */
        //Lucky: Travel on c's parent path and record what we see alone the way
        List<CbClass> InheritingPath = new List<CbClass>();
        CbClass ptr = c;
        do
        {
            InheritingPath.Add(ptr);
            ptr = ptr.Parent;
            if (ptr == CbType.Object)
            {
                //success
                break;
            }
            
            if (ptr == null)
            { 
                //fail
                Start.SemanticError(lineNumber, "Class inheriting path broken.");
                break;
            }

            foreach (CbClass ancestor in InheritingPath)
            {
                if (ptr == ancestor)
                {
                    Start.SemanticError(lineNumber, "Circlar inhertance detected.");
                    break;
                }
            }
        } while (true);
    }
    
    private bool isAssignmentCompatible(CbType dest, CbType src) {
        if (dest == CbType.Error || src == CbType.Error) return true;
        if (dest == src) return true;
        if (dest == CbType.Int) return isIntegerType(src);
        CbClass d = dest as CbClass;
        CbClass s = src as CbClass;
        if (d != null) {
            if (src == CbType.Null) return true;
            if (s == null) return false;
            if (isAncestor(d,s)) return true;
        }
        return false;
    }
    
    private void checkTypeSyntax(AST n) {
	if (n.Tag == NodeType.Array) {
		checkTypeSyntax(n[0]);
	} else {
		CbType thisType = null;
		switch(n.Tag) {
			case NodeType.IntType: n.Type = CbType.Int; break;
			case NodeType.StringType: n.Type = CbType.String; break;
			case NodeType.CharType: n.Type = CbType.Char; break;
			case NodeType.VoidType: n.Type = CbType.Void; break;
			case NodeType.Ident:
			{
			    AST_leaf identifier = n as AST_leaf;
			    thisType = ns.LookUp(identifier.Sval) as CbType;
			    if (thisType == null) {
			    	    Start.SemanticError(n.LineNumber,"unrecognized identifier {0}",identifier.Sval);
			    	    n.Type = CbType.Error;
			    } else {
			    	    n.Type = thisType;
			    }
			    break;
			}
			default:
			{
			    throw new Exception("Unexcepted tag in type parsing.");
			    n.Type = CbType.Error;
			}
		}					
	}
        /* TODO
           code to check whether n is the subtree that has appropriate AST
           structure for a Cb type. It could be a builtin type (int, char,
           string), a class, or an array whose elements have a valid type.
        */
    }

    private bool isCastable(CbType dest, CbType src) {
        if (isIntegerType(dest) && isIntegerType(src)) return true;
        if (dest == CbType.Error || src == CbType.Error) return true;
        CbClass d = dest as CbClass;
        CbClass s = src as CbClass;
        if (isAncestor(d,s)) return true;
        if (isAncestor(s,d)) return true;
        return false;
    }
    
    // returns true if type t can be used where an integer is needed
    private bool isIntegerType(CbType t) {
        return t == CbType.Int || t == CbType.Char || t == CbType.Error;
    }
    
    // tests if T1 == T2 or T1 is an ancestor of T2 in hierarchy
    private bool isAncestor( CbClass T1, CbClass T2 ) {
        while(T1 != T2) {
            T2 = T2.Parent;
            if (T2 == null) return false;
        }
        return true;
    }
    
    private void checkOverride(AST_nonleaf node) {
	string name = currentMethod.Name;
	NodeType child_indTag = node[node.NumChildren - 1].Tag;
	bool hasAncestor = false;
	CbClass checkAncestor = currentClass.Parent;
	do {
		if (checkAncestor == null) 
			break;
		if (checkAncestor.Members.ContainsKey(name)) {
			CbMember anc_curr = checkAncestor.Members[name];
			if (anc_curr is CbMethod) {
				hasAncestor = true;
				CbMethod anc_check = anc_curr as CbMethod;
				if (currentMethod.ResultType != anc_check.ResultType) {
					Start.SemanticError(node.LineNumber,"ancestor method with same name {0} but different signature",name);
					break;
				} else if (anc_check.IsStatic || currentMethod.IsStatic) {
					Start.SemanticError(node.LineNumber,"ancestor or current method, {0}, set to static",name);
					break;
				} else if (child_indTag != NodeType.Override) {
					Start.SemanticError(node.LineNumber,"ancestor found but current method not set to override");	
				}
			}
		}
		checkAncestor = checkAncestor.Parent;
	} while (true);
	if (!(hasAncestor) && (child_indTag == NodeType.Override))
		Start.SemanticError(node.LineNumber,"no ancestor method found to be overrident");
        // search for a member in any ancestor with same name
        /* TODO
           code to check whether any ancestor class contains a member with
           the same name. If so, it has to be a method with the identical
           signature.
           If there is a method with the same signature, then neither method
           is allowed to be static. (Not part of Cb language.)
           Otherwise, currentMethod must be flagged as override (not virtual).
        */
    }

    private void checkCall(AST_nonleaf ast)
    {
        Debug.Assert(ast.Tag == NodeType.Call);
        CbClass classContext = null;
        string methodIdentifier;
        //Step.1 pinning down the class context
        if (ast[0].Tag == NodeType.Ident)
        {
            classContext = currentClass;
            methodIdentifier = (ast[0] as AST_leaf).Sval;
        } else if (ast[0].Tag == NodeType.Dot)
        {
            if (ast[0].Type == CbType.Error) 
            {
                ast.Type = CbType.Error;
                return;
            }
            AST_nonleaf dotexpr = ast[0] as AST_nonleaf;
            if (!(dotexpr[0].Type is CbClass))
            {
                Debug.Assert(false);//this error should be caught in Dot expression
                ast.Type = CbType.Error;
            }
            classContext = dotexpr[0].Type as CbClass;
            methodIdentifier = (dotexpr[1] as AST_leaf).Sval;
        } else
        {
            Start.SemanticError(ast.LineNumber, "Unexpected {0} in function call", ast.Tag);
            ast.Type = CbType.Error;
            return;
        }
        //Step 2. Look up the method type in the class context
        CbMember methodmember = null;
        bool containsKey = classContext.Members.TryGetValue(methodIdentifier, out methodmember);
        if (containsKey == false)
        {
            Debug.Assert(false);//this error should be caught in Dot expression
            ast.Type = CbType.Error;
            return;
        }
        CbMethod method = methodmember as CbMethod;
        Debug.Assert(method != null);
        IList<CbType> formalArgList = method.ArgType;
        //Step 3. check the types of parameter list
        AST_kary ActualList = ast[1] as AST_kary;
        if (ActualList.NumChildren != formalArgList.Count)
        {
            Start.SemanticError(ast.LineNumber, "Parameter list size unmacth for method {0}: expected {1} parameters but get {2}",
                methodIdentifier, 
                formalArgList.Count, 
                ActualList.NumChildren);
            ast.Type = CbType.Error;
            return;
        }
        for (int i = 0; i < ActualList.NumChildren; ++ i)
        {
            if (isAssignmentCompatible(formalArgList[i], ActualList[i].Type) == false)
            {
                Start.SemanticError(ast.LineNumber, "The No.{0} parameter of method {1} unmatch. Expected {2} but got {3}",
                    i,
                    methodIdentifier,
                    formalArgList[i],
                    ActualList[i].Type);
                ast.Type = CbType.Error;
                return;
            }
        }
        //we passed all checks. Set the node type to be function return type
        ast.Type = method.ResultType;
    }
   

    private string printTagToStr(AST node)
    {      
        StringBuilder builder = new StringBuilder();
        StringWriter writer = new StringWriter(builder);
        writer.Write("{0}:{1}_{2}", node.Tag, node.LineNumber, node.Type);
        return builder.ToString();
    }
}

}


