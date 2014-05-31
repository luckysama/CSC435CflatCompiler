/* CbParser.y */

// The grammar shown in this file is INCOMPLETE!!
// It does not support class inheritance, it does not permit
// classes to contain methods (other than Main).
// Other language features may be missing too.
%partial
%namespace  CbCompiler.FrontEnd
%tokentype  Tokens

// All tokens which can be used as operators in expressions
// they are ordered by precedence level (lowest first)
%right      '='
%left       OROR
%left       ANDAND
%nonassoc   EQEQ NOTEQ PLUSPLUS MINUSMINUS
%nonassoc   '>' GTEQ '<' LTEQ
%left       '+' '-'
%left       '*' '/' '%'
%nonassoc	VARREF
%left       UMINUS CAST
%nonassoc	PAREN

// All other named tokens (i.e. the single character tokens are omitted)
// The order in which they are listed here does not matter.
%token 		Kwd_while Kwd_if Kwd_then Kwd_else Kwd_break Kwd_return //branching keywords
%token		Kwd_int Kwd_char Kwd_string Kwd_void Kwd_null Kwd_new//data type keywords
%token		Kwd_public Kwd_static Kwd_const	Kwd_virtual Kwd_override Kwd_class	 //class construct keywords
%token 		Kwd_using 									 //preprocessor	
%token 		Kwd_out										//formal parameter modifiers
//non-keywords lexer tokens
%token 		Ident Number StringConst CharConst
%token		OpChar MiscChar WhiteSpace //placeholder for lexer token output

%start Program

%%

/* *************************************************************************
   *                                                                       *
   *         PRODUCTION RULES AND ASSOCIATED SEMANTIC ACTIONS              *
   *                                                                       *
   ************************************************************************* */

Program:        UsingList ClassList
        ;

UsingList:      /* empty */
        |       Kwd_using Ident ';' UsingList
        ;

ClassList:	    ClassList ClassDecl
		|		ClassDecl
		;

ClassDecl:		Kwd_class Ident ClassInherit ClassBody
		;

ClassInherit:	':' Ident
		|		/* empty */
		;
		
ClassBody: 		'{' DeclList '}'
		;

DeclList:       DeclList ConstDecl
        |       DeclList MethodDecl
        |       DeclList FieldDecl
		| 		/* empty */
        ;

ConstDecl:      Kwd_public Kwd_const Type Ident '=' InitVal ';'
        ;

InitVal:        Number
        |       StringConst
		|		CharConst
        ;

FieldDecl:      Kwd_public Type IdentList ';'
        ;

IdentList:      IdentList ',' Ident
        |       Ident
        ;

MethodDecl:     Kwd_public MethodModifiers Type Ident '(' OptFormals ')' Block
		|       Kwd_public MethodModifiers Kwd_void Ident '(' OptFormals ')' Block
		|       Kwd_public Type Ident '(' OptFormals ')' Block
		|       Kwd_public Kwd_void Ident '(' OptFormals ')' Block
        ;

MethodModifiers: MethodModifiers Kwd_static
		|	MethodModifiers Kwd_virtual
		| 	MethodModifiers Kwd_override
		|   Kwd_static
		|   Kwd_virtual
		|   Kwd_override
		;

//MethodReturnType: Kwd_void
//		|	Type
//		;
		

OptFormals:     /* empty */
        |       FormalPars
        ;

FormalPars:     FormalDecl
        |       FormalPars ',' FormalDecl
        ;

FormalDecl:     Type Ident
        ;

Type:           Ident
        |       Ident '[' ']'
        |       Kwd_int
        |       Kwd_string
		|		Kwd_char
		|       Kwd_int '[' ']'
        |       Kwd_string '[' ']'
		|		Kwd_char '[' ']'
		;
		
Statement: MatchedStatement
		| UnmatchedStatement;

MatchedStatement: Kwd_if '(' Expr ')' MatchedStatement Kwd_else MatchedStatement    
		|		Designator '=' Expr ';'
        |       Designator '(' OptActuals ')' ';'
        |       Designator PLUSPLUS ';'
        |       Designator MINUSMINUS ';'
        |       Kwd_while '(' Expr ')' MatchedStatement
        |       Kwd_break ';'
        |       Kwd_return ';'
        |       Kwd_return Expr ';'
        |       Block
        |       ';'
        ;
		
UnmatchedStatement: Kwd_if '(' Expr ')' MatchedStatement
		| Kwd_if '(' Expr ')' UnmatchedStatement
		| Kwd_if '(' Expr ')' MatchedStatement Kwd_else UnmatchedStatement
		| Kwd_while '(' Expr ')' UnmatchedStatement
		;

OptActuals:     /* empty */
        |       ActPars
        ;

ActPars:        ActPars ',' Expr
        |       Expr
        ;

Block:          '{' DeclsAndStmts '}'
        ;

LocalDecl:      Type IdentList ';' //local decl does not have "public"
        ;

DeclsAndStmts:   /* empty */
        |       DeclsAndStmts Statement
        |       DeclsAndStmts LocalDecl
        ;

Expr:           Expr OROR Expr
        |       Expr ANDAND Expr
        |       Expr EQEQ Expr
        |       Expr NOTEQ Expr
        |       Expr LTEQ Expr
        |       Expr '<' Expr
        |       Expr GTEQ Expr
        |       Expr '>' Expr
        |       Expr '+' Expr
        |       Expr '-' Expr
        |       Expr '*' Expr
        |       Expr '/' Expr
        |       Expr '%' Expr
        |       '-' Expr %prec UMINUS
        |       Designator
        |       Designator '(' OptActuals ')'
        |       Number
        |       StringConst
		|		CharConst
        |       StringConst '.' '[' Ident ']'// Ident must be "Length"
        |       Kwd_new Ident '(' ')'
        |       Kwd_new Ident '[' Expr ']'
		|		Kwd_new Kwd_int '[' Expr ']'
		|		Kwd_new Kwd_string '[' Expr ']'
		|		Kwd_new Kwd_char '[' Expr ']'
        |       '(' Expr ')' %prec PAREN
		|		'(' Expr ')' Expr %prec CAST
		|		'(' Kwd_int ')' Expr %prec CAST
		|		'(' Kwd_string ')' Expr %prec CAST
		|		'(' Kwd_char ')' Expr %prec CAST
		|		Kwd_null
        ;

Designator:     Ident Qualifiers
		|		Ident
		; 

Qualifiers:     '.' Ident Qualifiers
        |       '[' Expr ']' Qualifiers
        |       '.' Ident
		|		'[' Expr ']'
        ;

%%




