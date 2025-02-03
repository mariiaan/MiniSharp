using MiniSharp;
using System.Reflection.Emit;
using System.Text.Json;

class Program
{

    public static void Main(string[] args)
    {
        NestedStruct3 nested3 = new NestedStruct3("Nested3Example", 10, true);
        NestedStruct4 nested4 = new NestedStruct4(0.15f, 123.45, "Category1");
        NestedStruct1 nested1 = new NestedStruct1(25.5f, "First Nested Struct", nested3);
        NestedStruct2 nested2 = new NestedStruct2(100.75, true, nested4);

        RootStruct root = new RootStruct("RootExample", nested1, nested2, 1, true);


        Minifile.Serialize(root, "MyNestedStructures").Write("endlich.mini");
    }
}


// Root struct
[MiniSerialize]
public struct RootStruct
{
    public string Name;
    public NestedStruct1 Nested1;
    public NestedStruct2 Nested2;
    public int Id;
    public bool IsActive;

    public RootStruct(string name, NestedStruct1 nested1, NestedStruct2 nested2, int id, bool isActive)
    {
        Name = name;
        Nested1 = nested1;
        Nested2 = nested2;
        Id = id;
        IsActive = isActive;
    }
}

// First level of nested structs
[MiniSerialize]
public struct NestedStruct1
{
    public float Value;
    public string Description;
    public NestedStruct3 Nested3;

    public NestedStruct1(float value, string description, NestedStruct3 nested3)
    {
        Value = value;
        Description = description;
        Nested3 = nested3;
    }
}

public struct NestedStruct2
{
    public double Amount;
    public bool IsValid;
    public NestedStruct4 Nested4;

    public NestedStruct2(double amount, bool isValid, NestedStruct4 nested4)
    {
        Amount = amount;
        IsValid = isValid;
        Nested4 = nested4;
    }
}

// Third level of nested structs
public struct NestedStruct3
{
    public string Name;
    public int Count;
    public bool IsEnabled;

    public NestedStruct3(string name, int count, bool isEnabled)
    {
        Name = name;
        Count = count;
        IsEnabled = isEnabled;
    }
}

public struct NestedStruct4
{
    public float Rate;
    public double Total;
    public string Category;

    public NestedStruct4(float rate, double total, string category)
    {
        Rate = rate;
        Total = total;
        Category = category;
    }
}
