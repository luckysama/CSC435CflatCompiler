// test of everything, but without a using clause

class Foo {
    public const int theAnswer = 42;
    public const string hiThere = "hello";
    public const char itsAnX = 'x';

    public int a;
    public string b;
    public char c;

    public static void Main() {
        Foo f;
        f = new Bar();
        int r;
        r = f.Umm(3,4);
		r = f.Ummm(3,4);
		r = f.Ummm(3,4,5);//should have a parameter list un-match
    }

    public virtual int Ummm( int a, int b ) {
        System.Console.WriteLine("This is Foo");
        return a+b;
    }

}


class Bar : Foo {
    public int x;

    public override int Umm( int aa, int bb ) {
        System.Console.WriteLine("This is Bar");
        return a-b;
    }
}
