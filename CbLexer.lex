%namespace CbCompiler.FrontEnd
%tokentype Tokens

%{
  public int lineNum = 1; //don't start from 0!!
  public int LineNumber { get{ return lineNum; } }
%}

%x comment
%x strcnst
space [ \t]
opchar [+\-*/=<>%] // must escape - as it signifies a range
otherchar [\{\}\(\)\[\]\.\";:,'`]
underscore "_"
%%
	int nested = 0;
"/*"	BEGIN(comment);	

<comment>[^*\n]*	{}
<comment>"*"+[^*/\n]*	{}
<comment>"/"+"*"	++nested;
<comment>\n		++lineNum;
<comment>"*"+"/"	{	if(nested == 0) {
					BEGIN(INITIAL);
				} else {
					--nested;
				}
			}	

\"		BEGIN(strcnst);

<strcnst>\\\"|[^\"]	{}
<strcnst> \" { BEGIN(INITIAL);last_token_text=yytext; SayToken(Tokens.StringConst, yytext); return (int)Tokens.StringConst;}	

\'.\'		 {last_token_text=yytext; SayToken(Tokens.CharConst, yytext[1]); return (int)Tokens.CharConst; }
{space}      { /* SayToken(Tokens.WhiteSpace, yytext[0]); */ }
"using"		 {last_token_text=yytext; SayToken(Tokens.Kwd_using); return (int)Tokens.Kwd_using;}
"void"		 {last_token_text=yytext; SayToken(Tokens.Kwd_void); return (int)Tokens.Kwd_void;}
"return"	 {last_token_text=yytext; SayToken(Tokens.Kwd_return); return (int)Tokens.Kwd_return;}
"class"		 {last_token_text=yytext; SayToken(Tokens.Kwd_class); return (int)Tokens.Kwd_class;}
"else"		 {last_token_text=yytext; SayToken(Tokens.Kwd_else); return (int)Tokens.Kwd_else;}
"static"	 {last_token_text=yytext; SayToken(Tokens.Kwd_static); return (int)Tokens.Kwd_static;}
"public"	 {last_token_text=yytext; SayToken(Tokens.Kwd_public); return (int)Tokens.Kwd_public;}
"const"		 {last_token_text=yytext; SayToken(Tokens.Kwd_const); return (int)Tokens.Kwd_const;}
"break"		 {last_token_text=yytext; SayToken(Tokens.Kwd_break); return (int)Tokens.Kwd_break;}
"if"		 {last_token_text=yytext; SayToken(Tokens.Kwd_if); return (int)Tokens.Kwd_if;}
"new"		 {last_token_text=yytext; SayToken(Tokens.Kwd_new); return (int)Tokens.Kwd_new;}
"out"		 {last_token_text=yytext; SayToken(Tokens.Kwd_out); return (int)Tokens.Kwd_out;}
"override"	 {last_token_text=yytext; SayToken(Tokens.Kwd_override); return (int)Tokens.Kwd_override;}
"virtual"	 {last_token_text=yytext; SayToken(Tokens.Kwd_virtual); return (int)Tokens.Kwd_virtual;}
"while"		 {last_token_text=yytext; SayToken(Tokens.Kwd_while); return (int)Tokens.Kwd_while;}
"null"		 {last_token_text=yytext; SayToken(Tokens.Kwd_null); return (int)Tokens.Kwd_null;}
"char"		 {last_token_text=yytext; SayToken(Tokens.Kwd_char); return (int)Tokens.Kwd_char;}
"int"		 {last_token_text=yytext; SayToken(Tokens.Kwd_int); return (int)Tokens.Kwd_int;}
"string"	 {last_token_text=yytext; SayToken(Tokens.Kwd_string); return (int)Tokens.Kwd_string;}
"=="		 {last_token_text=yytext; SayToken(Tokens.EQEQ); return (int)Tokens.EQEQ;}
"!="		 {last_token_text=yytext; SayToken(Tokens.NOTEQ); return (int)Tokens.NOTEQ;}
">="		 {last_token_text=yytext; SayToken(Tokens.GTEQ); return (int)Tokens.GTEQ;}
"<="		 {last_token_text=yytext; SayToken(Tokens.LTEQ); return (int)Tokens.LTEQ;}
"++"		 {last_token_text=yytext; SayToken(Tokens.PLUSPLUS); return (int)Tokens.PLUSPLUS;}
"&&"		 {last_token_text=yytext; SayToken(Tokens.ANDAND); return (int)Tokens.ANDAND;}
"||"		 {last_token_text=yytext; SayToken(Tokens.OROR); return (int)Tokens.OROR;}

0|[1-9][0-9]*    {last_token_text=yytext; SayToken(Tokens.Number, yytext); return (int)Tokens.Number;}
[a-zA-Z_][a-zA-Z0-9_]*            {last_token_text=yytext; SayToken(Tokens.Ident, yytext); return (int)Tokens.Ident;}
"//"\n		 {}

{opchar}         { SayToken(Tokens.OpChar, yytext[0]); return (int)(yytext[0]);}
{otherchar}		 { SayToken(Tokens.MiscChar, yytext[0]); return (int)(yytext[0]);}
"\n"             { /* SayToken(Tokens.WhiteSpace, "\\n"); */ lineNum ++; }
"\r\n"           { /* SayToken(Tokens.WhiteSpace, "\\r\\n"); */ lineNum ++;}

.                { yyerror("illegal character ({0})", yytext); }

%%

public string last_token_text = "";
