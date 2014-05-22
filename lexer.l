%namespace LexScanner
%tokentype Tokens

%{
  public int lineNum = 1;

  public int LineNumber { get{ return lineNum; } }

  public override void yyerror( string msg, params object[] args ) {
    Console.WriteLine("{0}: ", lineNum);
    if (args == null || args.Length == 0) {
      Console.WriteLine("{0}", msg);
    }
    else {
      Console.WriteLine(msg, args);
    }
  }

  public void yyerror( int lineNum, string msg, params object[] args ) {
    Console.WriteLine("{0}: {1}", msg, args);
  }

%}

space [ \t]
opchar [+\-*/] // must escape - as it signifies a range
underscore "_"
%%
{space}          {}
0|[1-9][0-9]*    {last_token_text=yytext;return (int)Tokens.NUM;}
0x[1-9a-fA-F]*		 {last_token_text=yytext;return (int)Tokens.HEXNUM;}
[a-zA-Z_][a-zA-Z0-9_]*            {last_token_text=yytext;return (int)Tokens.IDENT;}

{opchar}         {return (int)(yytext[0]);}
"\n"             {return (int)'\n';}
"\r\n"           {return (int)'\n';}

.                { yyerror("illegal character ({0})", yytext); }

%%

public string last_token_text = "";

