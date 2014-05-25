%namespace LexScanner
%tokentype Tokens

%{
  public int lineNum = 1;

  public int LineNumber { get{ return lineNum; } }

  public override void yyerror( string msg, params object[] args ) { Console.WriteLine("{0}: ", lineNum); if
    (args == null || args.Length == 0) { Console.WriteLine("{0}", msg); } else { Console.WriteLine(msg, args); }
    }

  public void yyerror( int lineNum, string msg, params object[] args ) {
    Console.WriteLine("{0}: {1}", msg, args);
  }

%}

space [ \t]
opchar [+\-*/=<>] // must escape - as it signifies a range
underscore "_"
%%
{space}          {}
"using"		 {last_token_text=yytext;return (int)Tokens.Kwd_using;}
"void"		 {last_token_text=yytext;return (int)Tokens.Kwd_void;}
"return"	 {last_token_text=yytext;return (int)Tokens.Kwd_return;}
"class"		 {last_token_text=yytext;return (int)Tokens.Kwd_class;}
"else"		 {last_token_text=yytext;return (int)Tokens.Kwd_else;}
"static"	 {last_token_text=yytext;return (int)Tokens.Kwd_static;}
"public"	 {last_token_text=yytext;return (int)Tokens.Kwd_public;}
"const"		 {last_token_text=yytext;return (int)Tokens.Kwd_if;}
"break"		 {last_token_text=yytext;return (int)Tokens.Kwd_break;}
"if"		 {last_token_text=yytext;return (int)Tokens.Kwd_if;}
"new"		 {last_token_text=yytext;return (int)Tokens.Kwd_new;}
"out"		 {last_token_text=yytext;return (int)Tokens.Kwd_out;}
"while"		 {last_token_text=yytext;return (int)Tokens.Kwd_while;}
"=="		 {last_token_text=yytext;return (int)Tokens.EQEQ;}
"!="		 {last_token_text=yytext;return (int)Tokens.NOTEQ;}
">="		 {last_token_text=yytext;return (int)Tokens.GTEQ;}
0|[1-9][0-9]*    {last_token_text=yytext;return (int)Tokens.Number;}
"++"		 {last_token_text=yytext;return (int)Tokens.PLUSPLUS;}
0x[1-9a-fA-F]*		 {last_token_text=yytext;return (int)Tokens.HEXNUM;}
[a-zA-Z_][a-zA-Z0-9_]*            {last_token_text=yytext;return (int)Tokens.Ident;}

{opchar}         {return (int)(yytext[0]);}
"\n"             {return (int)'\n';}
"\r\n"           {return (int)'\n';}

.                { yyerror("illegal character ({0})", yytext); }

%%

public string last_token_text = "";
