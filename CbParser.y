/* CbParser.y */

// The grammar shown in this file is INCOMPLETE!!
// It does not support class inheritance, it does not permit
// classes to contain methods (other than Main).
// Other language features may be missing too.

%namespace  FrontEnd
%tokentype  Tokens

// All tokens which can be used as operators in expressions
// they are ordered by precedence level (lowest first)
%right      '='
%left       OROR
%left       ANDAND
%nonassoc   EQEQ NOTEQ
%nonassoc   '>' GTEQ '<' LTEQ
%left       '+' '-'
%left       '*' '/' '%'
%left       UMINUS

// All other named tokens (i.e. the single character tokens are omitted)
// The order in which they are listed here does not matter.
%token 		Kwd_while Kwd_if Kwd_else Kwd_break Kwd_return //branching keywords
%token		Kwd_int Kwd_char Kwd_string Kwd_void Kwd_null //data type keywords
%token		Kwd_public Kwd_static Kwd_const	Kwd_virtual Kwd_override Kwd_class	 //class construct keywords
%token 		Kwd_using 									 //preprocessor	


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
        ;

ConstDecl:      Kwd_public Kwd_const Type Ident '=' InitVal ';'
        ;

InitVal:        Number
        |       StringConst
        ;

FieldDecl:      Kwd_public Type IdentList ';'
        ;

IdentList:      IdentList ',' Ident
        |       Ident
        ;

MethodDecl:     Kwd_public MethodModifiers MethodReturnType Ident '(' OptFormals ')' Block
        ;

MethodModifiers: MethodModifiers Kwd_static
		|	MethodModifiers Kwd_virtual
		| 	MethodModifiers Kwd_override
		|	/* empty */
		;

MethodReturnType: Kwd_void
		|	Ident
		;
		

OptFormals:     /* empty */
        |       FormalPars
        ;

FormalPars:     FormalDecl
        |       FormalPars ',' FormalDecl
        ;

FormalDecl:     Type Ident
        ;

Type:           TypeName
        |       TypeName '[' ']'
        ;

TypeName:       Ident
        |       Kwd_int
        |       Kwd_string
		|		Kwd_char
        ;

Statement:      Designator '=' Expr ';'
        |       Designator '(' OptActuals ')' ';'
        |       Designator PLUSPLUS ';'
        |       Designator MINUSMINUS ';'
        |       Kwd_if '(' Expr ')' Statement OptElsePart
        |       Kwd_while '(' Expr ')' Statement
        |       Kwd_break ';'
        |       Kwd_return ';'
        |       Kwd_return Expr ';'
        |       Block
        |       ';'
        ;

OptActuals:     /* empty */
        |       ActPars
        ;

ActPars:        ActPars ',' Expr
        |       Expr
        ;

OptElsePart:    Kwd_else Statement
        |       /* empty */
        ;

Block:          '{' DeclsAndStmts '}'
        ;

LocalDecl:      Ident IdentList ';' //local decl does not have "public"
        |       Ident '[' ']' IdentList ';'
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
        |       StringConst '.' '[' Ident ']'// Ident must be "Length"
        |       Kwd_new Ident '(' ')'
        |       Kwd_new Ident '[' Expr ']'
        |       '(' Expr ')'
		|		Kwd_null
        ;

Designator:     Ident Qualifiers
        ;

Qualifiers:     '.' Ident Qualifiers
        |       '[' Expr ']' Qualifiers
        |       /* empty */
        ;

%%




