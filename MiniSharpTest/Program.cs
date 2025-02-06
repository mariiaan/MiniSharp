using MiniSharp;

class Program
{
    public static void Main(string[] args)
    {
        {
            MainInstance m = new MainInstance();
            TestNested k = new TestNested();
            m.Test4 = k;
            m.Test3 = true;
            m.Test2 = 5;
            m.Test1 = "Hallo";
            k.Test1 = "Kacka";
            k.Test2 = 3;
            k.Test3 = false;
            
            var sw = new StreamWriter("finally.mini");
            Minifile.Serialize(m, "MyNestedStructures").Write(sw);
            sw.Close();
        }
        {
            Minifile ff = new Minifile();
            ff.Parse(new StreamReader("finally.mini"));

            MainInstance root = Minifile.Deserialize<MainInstance>(ff);
        }
    }
}

// Some test structs for reflection
// Root struct
[MiniSerialize]
public class MainInstance
{
    public string Test1;
    public int Test2;
    public bool Test3;
    public TestNested Test4;

    public MainInstance() { }
}

[MiniSerialize]
public class TestNested
{
    public string Test1;
    public int Test2;
    public bool Test3;
    public TestNested() { }
}