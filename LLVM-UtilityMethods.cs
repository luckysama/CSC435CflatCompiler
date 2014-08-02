/* LLVM-UtilityMethods.cs
 * 
 * Utility code to help with outputting intermediate code in the
 * LLVM text format (as a '.ll' file).
 *
 * These are the simpler utility methods
 * 
 * Author: Nigel Horspool
 * Date: July 2014
 */
 
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;


namespace FrontEnd
{
        
    public partial class LLVM
    {
        int nextBBNumber = 0;       // used to number basic blocks
        int nextUnnamedIndex = -1;  // used to generate %0, %1, %2 ... sequences
        bool basicblockend = false; //lucky: prevent any output until a new label

        // generates a unique name for a basic block label
        public string CreateBBLabel(string prefix="label")
        {
            return prefix + "." + nextBBNumber++;
        }

        public string nextTemporary() {
            return "%" + nextUnnamedIndex++;
        }

        // Given a reference to memory, this generated a load to get the value
        // into a LLVM temporary
        public LLVMValue Dereference(LLVMValue src)
        {
            if (basicblockend == true)
            {
                Start.WarningMessage(-1, "Basic block ended, instruction omitted.(dereference).");
                return src;
            }
            if (!src.IsReference) return src;
            string rv = nextTemporary();
            ll.WriteLine("  {0} = load {1}* {2}", rv, src.LLType, src.LLValue);
            return new LLVMValue(src.LLType, rv, false);
        }

        // Convert the operand into an i32 LLVM value in a temporary
        public LLVMValue ForceIntValue(LLVMValue src)
        {
            if (basicblockend == true)
            {
                Start.WarningMessage(-1, "Basic block ended, instruction omitted.(forceintvalue).");
                return src;
            }
            
            string rv;
            src = Dereference(src);
            if (src.LLType == "i32")
                return src;
            if (src.LLType == "i8")
            {
                rv = nextTemporary();
                ll.WriteLine("  {0} = zext i8 {1} to i32", rv, src.LLValue);
                return new LLVMValue("i32", rv, false);
            }
            throw new Exception("unhandled case for LLVM.ForceIntValue");
        }

        // Generates a memory reference to a field of a class instance
        public LLVMValue RefClassField( LLVMValue instancePtr, CbField field ) {
            if (basicblockend == true)
            {
                Start.WarningMessage(-1, "Basic block ended, instruction omitted.(RefClassfield).");
                return instancePtr;
            }

            string rv = nextTemporary();
            ll.WriteLine("  {0} = getelementptr inbounds {1}, i32 0, i32 {2}",
                rv, instancePtr, field.Index);
            return new LLVMValue(GetTypeDescr(field.Type), rv, true);
        }

        // stores a LLVM temporary into memory
       public void Store( LLVMValue source, LLVMValue dest ) {
           if (basicblockend == true)
           {
               Start.WarningMessage(-1, "Basic block ended, instruction omitted.(store).");
               return;
           }

            if (!dest.IsReference)
                throw new Exception("LLVM.Store needs a memory reference for the dest");
            source = Dereference(source);
            string srcType = source.LLType;
            string destType = dest.LLType;
            int align;
            if (destType == "i8") align = 1;
            else if (destType.EndsWith("*")) align = ptrAlign;
            else align = 4;
            ll.WriteLine("  store {0}, {1}* {2}, align {3}",
                source, dest.LLType, dest.LLValue, align);
        }

        public void WriteReturnInst(LLVMValue result)
        {
            if (basicblockend == true)
            {
                Start.WarningMessage(-1, "Basic block ended, instruction omitted.(return).");
                return;
            }

            if (result == null)
                ll.WriteLine("  ret void");
            else
                ll.WriteLine("  ret {0}", Dereference(result));
        }

        // outputs a label
        public void WriteLabel(string name)
        {
            basicblockend = false; //the start of another block
            ll.WriteLine(name + ":");
        }

        // outputs an unconditional branch
       public void WriteBranch(string lab)
        {
            if (basicblockend == true)
            {
                Start.WarningMessage(-1, "Basic block ended, instruction omitted.(unconditional jump).");
                return;
            } 
           ll.WriteLine("  br label %{0}", lab);
            basicblockend = true;
        }

        // outputs a conditional branch
        public void WriteCondBranch(LLVMValue cond, string trueDest, string falseDest)
        {
            if (basicblockend == true)
            {
                Start.WarningMessage(-1, "Basic block ended, instruction omitted.(conditional jump).");
                return;
            }
            Debug.Assert(cond.LLType == "i1");
            ll.WriteLine("  br i1 {0}, label %{1}, label %{2}",
                cond.LLValue, trueDest, falseDest);
            basicblockend = true;
        }

        // Outputs an LLVM instruction which has two int operands and produces int result
        public LLVMValue WriteIntInst(string opcode, LLVMValue lhs, LLVMValue rhs)
        {
            if (basicblockend == true)
            {
                Start.WarningMessage(-1, "Basic block ended, instruction omitted.(IntInst).");
                return lhs;
            }
            
            lhs = ForceIntValue(lhs);
            rhs = ForceIntValue(rhs);
            string rv = nextTemporary();
            ll.WriteLine("  {0} = {1} i32 {2}, {3}", rv, opcode, lhs.LLValue, rhs.LLValue);
            return new LLVMValue("i32", rv, false);
        }

  
        // Outputs an LLVM instruction which has two int operands and produces int result
        // It uses the AST node tag to select the appropriate instruction
        public LLVMValue WriteIntInst(NodeType tag, LLVMValue lhs, LLVMValue rhs)
        {
            string op = null;
            switch (tag)
            {
                case NodeType.Add: op = "add"; break;
                case NodeType.Sub: op = "sub"; break;
                case NodeType.Mul: op = "mul"; break;
                case NodeType.Div: op = "sdiv"; break;
                case NodeType.Mod: op = "srem"; break;
            }
            Debug.Assert(op != null);
            return WriteIntInst(op, lhs, rhs);
        }

       // compare two int or char values -- comparing two different kinds of pointer is unsupported
        public LLVMValue WriteCompInst(string cmp, LLVMValue lhs, LLVMValue rhs)
        {

            if (basicblockend == true)
            {
                Start.WarningMessage(-1, "Basic block ended, instruction omitted.(cmpinst).");
                return lhs;
            }
            string rv;
            lhs = Dereference(lhs);
            rhs = Dereference(rhs);
            if (lhs.LLType == "i8" && rhs.LLType == "i32")
            {
                lhs = ForceIntValue(lhs);
            }
            else if (lhs.LLType == "i32" && rhs.LLType == "i8")
            {
                rhs = ForceIntValue(rhs);
            }
            // we are comparing two i8 or two i32 values here
            rv = nextTemporary();
            ll.WriteLine("  {0} = icmp {1} i{4} {2}, {3}", rv, cmp,
                lhs.LLValue, rhs.LLValue, lhs.LLType=="i32"? 32:8 );
            return new LLVMValue("i1", rv, false);
        }

       // compare two int or char values -- comparing two different kinds of pointer is unsupported
       // The AST node tag selects the appropriate kind of comparison
        public LLVMValue WriteCompInst(NodeType tag, LLVMValue lhs, LLVMValue rhs)
        {
            string cmp = null;
            switch (tag)
            {
                case NodeType.Equals: cmp = "eq"; break;
                case NodeType.NotEquals: cmp = "ne"; break;
                case NodeType.GreaterOrEqual: cmp = "sge"; break;
                case NodeType.GreaterThan: cmp = "sgt"; break;
                case NodeType.LessOrEqual: cmp = "sle"; break;
                case NodeType.LessThan: cmp = "slt"; break;
            }
            Debug.Assert(cmp != null);
            return WriteCompInst(cmp, lhs, rhs);
        }

        #region Assignment 4 Utility Methods
        public LLVMValue WriteIntInst_LiteralConst(string opcode, LLVMValue lhs, int rhs)
        {
            if (basicblockend == true)
            {
                Start.WarningMessage(-1, "Basic block ended, instruction omitted.(writeIntInst_literal).");
                return lhs;
            }
            lhs = ForceIntValue(lhs);
            string rv = nextTemporary();
            string IntLiteral = rhs.ToString();
            ll.WriteLine("  {0} = {1} i32 {2}, {3}", rv, opcode, lhs.LLValue, IntLiteral);
            return new LLVMValue("i32", rv, false);
        }

        public LLVMValue WriteIntInst_LiteralConst(string opcode, int lhs, LLVMValue rhs)
        {
            if (basicblockend == true)
            {
                Start.WarningMessage(-1, "Basic block ended, instruction omitted.(writeIntInst_literal).");
                return rhs;
            }
            rhs = ForceIntValue(rhs);
            string rv = nextTemporary();
            string IntLiteral = lhs.ToString();
            ll.WriteLine("  {0} = {1} i32 {2}, {3}", rv, opcode, IntLiteral, rhs.LLValue);
            return new LLVMValue("i32", rv, false);
        }

        public LLVMValue WriteCmpInst_LiteralConst(string opcode, LLVMValue lhs, int rhs)
        {
            if (basicblockend == true)
            {
                Start.WarningMessage(-1, "Basic block ended, instruction omitted.(writeCmpInst_literal).");
                return lhs;
            }
            string rv = nextTemporary();
            string IntLiteral = rhs.ToString();
            ll.WriteLine("  {0} = icmp {1} i{4} {2}, {3}", rv, opcode,
               lhs.LLValue, IntLiteral, lhs.LLType == "i32" ? 32 : 8);
            return new LLVMValue("i1", rv, false);
        }

        public void WriteInitialize(LLVMValue lhs, string rhs_literalConst)
        {
            string alignspec;
            string literalConstSpec;
            if (lhs.LLType.StartsWith("i32"))
            {
                alignspec = ", align 4";
                literalConstSpec = "i32";
            } else
            {
                alignspec = ", align 1";
                literalConstSpec = "i8";
            }
           
                ll.WriteLine("  {0} = load {1} {2}{3}", lhs.LLValue, literalConstSpec, rhs_literalConst, alignspec);
           
       }

        #endregion

	}
}
