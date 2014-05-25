%using System.IO;

// Token declarations
%token NUM
%token HEXNUM
%token IDENT
%token ASSIGN

/* operator precedences & associativities in expressions */

%right '='
%left '-' '+'
%left '*' '/' '
%left UMINUS
%right '^'

// Start parsing at rule Statement
%start StatementList

%{
  public void yyerror( string format, params Object[] args ) {
    Console.Write("{0}: ", 99);//LineNumber);
    Console.WriteLine(format, args);
  }
%}

%%

StatementList:
      StatementList Statement '\n' {writeln(";----");}
    | 
    ;
Statement:
      Expr
    |
    ;
Expr:
      Expr '+' Expr     {writeln("add");}
    | Expr '-' Expr     {writeln("sub");}
    | Expr '*' Expr     {writeln("mul");}
    | Expr '/' Expr     {writeln("div");}
    | Factor
    ;
Factor:
      Atom
    ;
Atom: NUM               {writeln("ldc",token_text());}
	| HEXNUM			{writeln("ldxHEX", token_text());}
    | Ident
    ;
Ident:
      IDENT             {push_id();writeln("ldi",pop_id());} 
    ;

%%

Stack<string> id_stack = new Stack<string>();

void push_id() {
	string t = ((LexScanner.Scanner)Scanner).last_token_text;
	id_stack.Push(t);
}
string pop_id() {
	return id_stack.Pop();
}

string token_text() {
	return ((LexScanner.Scanner)Scanner).last_token_text;
}

void writeln() {
	writeln(null,null);
}
void writeln(string opcode) {
	writeln(opcode,null);
}

void writeln(string opcode, string value) {
	if (opcode != null) {
		System.Console.Write(opcode);
		if (value != null) {
			System.Console.Write(' '+value);
		}
	}
	System.Console.Write('\n');
}

// The parser needs a constructor
Parser() : base(null) { }

static void Main(string[] args)
{
	Parser parser = new Parser();

	FileStream file = new FileStream(args[0], FileMode.Open);
	parser.Scanner = new LexScanner.Scanner(file);
	System.Console.WriteLine("File: " + args[0]);

	parser.Parse();
}



