using System;

class List {
    public List next;
    public virtual void Print() {}
}

class Other: List {
    public char c;
	public char c; //This is a name conflict and should be reported
    public override void Print() {
        Console.Write(' ');
        Console.Write(c);
        if (next != null) next.Print();
    }
}

class Other //this is a class name conflict
{
public int c;//these are conflicting lines, but should not cause crash
public override void Print() { }
public int ShouldNotInSymbolTable; //this symbol should not appear in symbol table, because the class name is conflicting.
}

class Digit: List {
    public int d;
    public override void Print() {
        Console.Write(' ');
        Console.Write(d);
        if (next != null) next.Print();
    }
}

class Lists {
    public static void Main() {
        List ccc;
        string s;
        Console.WriteLine("enter some text => ");
        s = Console.ReadLine();
        ccc = null;
        int i;
        i = 0;
        while(i < s.Length) {
            char ch;
            ch = s[i];  i++;
            List elem;
            if (ch >= '0' && ch <= '9') {
		        Digit elemD;
                elemD = new Digit();  elemD.d = ch - '0';
                elem = elemD;
            } else {
		        Other elemO;
                elemO = new Other();  elemO.c = ch;
                elem = elemO;
            }
            elem.next = ccc;
            ccc = elem;
        }
        Console.WriteLine("\nReversed text =");
        ccc.Print();
        Console.WriteLine("\n");
    }
}
