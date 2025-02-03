using MiniSharp;
using MiniSharp.Values;
using System.Reflection;

namespace MiniSharp
{
    public enum EIntStyle
    {
        Decimal,
        Hexadecimal,
        Binary
    }

    public abstract class Value
    {
        private List<string> m_comments = new List<string>();

        public List<string> Comments
        {
            get { return m_comments; }
        }

        public abstract void Parse(string value);
        public abstract string ToString();

        public static Value ParseValue(string value)
        {
            char valueFirstChar = value[0];
            char valueLastChar = value[value.Length - 1];
            if (valueFirstChar == '"')
            {
                if (valueLastChar != '"')
                    throw new FormatException("Mismatched quotes in string value");

                value = value.Substring(1, value.Length - 2);
                StringValue sv = new StringValue();
                sv.Parse(value);

                return sv;
            }
            else if (valueLastChar == 'e')
            {
                var boolValue = new BooleanValue();
                boolValue.Parse(value);
                return boolValue;
            }
            else if (valueLastChar == 'f')
            {
                var floatValue = new FloatValue();
                floatValue.Parse(value);
                return floatValue;
            }
            else if (valueLastChar == ']')
            {
                var arrayValue = new ArrayValue();
                arrayValue.Parse(value);
                return arrayValue;
            }    
            else
            {
                var intValue = new IntValue();
                intValue.Parse(value);
                return intValue;
            }
        }
    }

    namespace Values
    {
        public class StringValue : Value
        {
            private string m_value = string.Empty;

            public StringValue() { }
            public StringValue(string value)
            {
                m_value = value;
            }

            public override void Parse(string value)
            {
                m_value = "";

                for (int i = 0; i < value.Length; ++i)
                {
                    if (value[i] == '\\')
                    {
                        if (i + 1 >= value.Length)
                        {
                            throw new FormatException("Invalid escape sequence");
                        }

                        switch (value[i + 1])
                        {
                            case '\"':
                                m_value += '\"';
                                break;
                            case 'n':
                                m_value += '\n';
                                break;
                            case 't':
                                m_value += '\t';
                                break;
                            case 'r':
                                m_value += '\r';
                                break;
                            case '\\':
                                m_value += '\\';
                                break;
                            default:
                                throw new FormatException("Invalid escape sequence");
                        }
                        ++i;
                    }
                    else if (value[i] == '\"')
                    {
                        throw new FormatException("Invalid character in string");
                    }
                    else
                    {
                        m_value += value[i];
                    }
                }
            }

            public override string ToString()
            {
                string sanitizedValue = m_value;

                for (int i = 0; i < sanitizedValue.Length; ++i)
                {
                    char currentChar = sanitizedValue[i];

                    switch (currentChar)
                    {
                        case '\n':
                            {
                                sanitizedValue = Utils.Replace(sanitizedValue, i, "\\n");
                                ++i;
                                break;
                            }
                        case '\t':
                            {
                                sanitizedValue = Utils.Replace(sanitizedValue, i, "\\t");
                                ++i;
                                break;
                            }
                        case '\r':
                            {
                                sanitizedValue = Utils.Replace(sanitizedValue, i, "\\r");
                                ++i;
                                break;
                            }
                        case '\\':
                            {
                                sanitizedValue = sanitizedValue.Insert(i, "\\");
                                ++i;
                                break;
                            }
                        case '\"':
                            {
                                sanitizedValue = sanitizedValue.Insert(i, "\\");
                                ++i;
                                break;
                            }

                        default: break;
                    }
                }

                m_value = "\"" + sanitizedValue + "\"";
                return m_value;
            }

            public string Value
            {
                get { return m_value; }
            }
        }

        public class IntValue : Value
        {
            public IntValue() { }
            public IntValue(long value, EIntStyle? style = EIntStyle.Decimal)
            {
                m_value = value;
                if (style != null)
                    m_style = style.Value;
            }

            private long m_value = 0;
            private EIntStyle m_style = EIntStyle.Decimal;

            public override void Parse(string value)
            {
                value = value.Replace("_", "");
                if (value.Length == 0)
                {
                    throw new FormatException("Empty integer value");
                }
                char lastChar = value[^1];

                if (lastChar == 'h')
                {
                    m_value = Convert.ToInt64(value[..^1], 16);
                    m_style = EIntStyle.Hexadecimal;
                }
                else if (lastChar == 'b')
                {
                    m_value = Convert.ToInt64(value[..^1], 2);
                    m_style = EIntStyle.Binary;
                }
                else
                {
                    m_value = Convert.ToInt64(value);
                    m_style = EIntStyle.Decimal;
                }
            }

            public override string ToString()
            {
                return m_style switch
                {
                    EIntStyle.Decimal => m_value.ToString(),
                    EIntStyle.Hexadecimal => Convert.ToString(m_value, 16) + "h",
                    EIntStyle.Binary => Convert.ToString(m_value, 2) + "b",
                    _ => throw new FormatException("Invalid integer style"),
                };
            }

            public long Value
            {
                get { return m_value; }
            }
        }

        public class BooleanValue : Value
        {
            public BooleanValue() { }
            public BooleanValue(bool value)
            {
                m_value = value;
            }

            private bool m_value = false;

            public override void Parse(string value)
            {
                if (value == "true")
                {
                    m_value = true;
                }
                else if (value == "false")
                {
                    m_value = false;
                }
                else
                {
                    throw new FormatException("Invalid boolean value");
                }
            }

            public override string ToString()
            {
                return m_value ? "true" : "false";
            }

            public bool Value
            {
                get { return m_value; }
            }
        }

        public class FloatValue : Value
        {
            public FloatValue() { }
            public FloatValue(double value)
            {
                m_value = value;
            }

            private double m_value = 0.0;

            public override void Parse(string value)
            {
                m_value = Convert.ToDouble(value.Substring(0, value.Length - 1), System.Globalization.CultureInfo.InvariantCulture);
            }

            public override string ToString()
            {
                return m_value.ToString();
            }

            public double Value
            {
                get { return m_value; }
            }
        }

        public class ArrayValue : Value
        {
            private List<Value> m_values = new List<Value>();

            public override void Parse(string value)
            {
                if (value[0] != '[' || value[value.Length - 1] != ']')
                {
                    throw new FormatException("Array value must be enclosed in [] brackets");
                }

                int bracketCounter = 0;
                bool isInString = false;

                List<string> elements = new List<string>();
                string currentElement = "";

                for (int i = 0; i < value.Length; ++i)
                {
                    char c = value[i];

                    if (isInString)
                    {
                        if (c == '\\')
                        {
                            if (i + 1 >= value.Length)
                            {
                                throw new FormatException("Invalid escape sequence");
                            }
                            currentElement += c;
                            currentElement += value[i + 1];
                            ++i;
                        }
                        else if (c == '"')
                        {
                            isInString = false;
                            currentElement += c;
                        }
                        else
                        {
                            currentElement += c;
                        }
                    }
                    else if (c == '\\')
                        ++i;
                    else if (c == '"')
                    {
                        currentElement += c;
                        isInString = true;
                    }
                    else
                    {
                        if (c == '[')
                        {
                            if (++bracketCounter > 1)
                                currentElement += c;
                        }
                        else if (c == ']')
                        {
                            --bracketCounter;
                            if (bracketCounter < 0)
                                throw new FormatException("Mismatched brackets in array value");
                            else if (bracketCounter >= 1)
                                currentElement += c;
                        }
                        else if (c == ',' && bracketCounter == 1)
                        {
                            elements.Add(currentElement);
                            currentElement = "";
                        }
                        else if (c != ' ' && c != '\t')
                            currentElement += c;
                    }
                }

                if (bracketCounter != 0)
                    throw new FormatException("Mismatched brackets in array value");

                if (currentElement.Length > 0)
                    elements.Add(currentElement);

                Type? lastType = null;

                foreach (var element in elements)
                {
                    var parsedValue = ParseValue(element);
                    if (parsedValue == null)
                        throw new FormatException("Invalid array element");

                    if (lastType == null)
                        lastType = parsedValue.GetType();
                    else if (lastType != parsedValue.GetType())
                        throw new FormatException("Array elements must be of the same type");

                    m_values.Add(parsedValue);
                }
            }

            public override string ToString()
            {
                Type? lastType = null;

                StringWriter sw = new StringWriter();
                foreach (var val in m_values)
                {
                    string valStr = val.ToString();

                    if (lastType == null)
                        lastType = val.GetType();
                    else if (lastType != val.GetType())
                        throw new FormatException("Array elements must be of the same type");

                    sw.Write(valStr);
                    sw.Write(", ");
                }

                string result = sw.ToString();
                if (result.Length > 0)
                    result = result.Substring(0, result.Length - 2);

                return "[" + result + "]";
            }

            public List<Value> Value
            {
                get { return m_values; }
            }
        }
    }

    public class Section
    {
        private Dictionary<string, Value> m_values = new Dictionary<string, Value>();
        private Dictionary<string, Section> m_subSections = new Dictionary<string, Section>();
        private List<string> m_comments = new List<string>();

        public List<string> Comments
        {
            get { return m_comments; }
        }

        public Dictionary<string, Value> Values
        {
            get { return m_values; }
        }

        public Dictionary<string, Section> SubSections
        {
            get { return m_subSections; }
        }

        public void SetSubSection(string name, Section section, bool allowOverwrite = false)
        {
            if (m_subSections.ContainsKey(name))
            {
                if (!allowOverwrite)
                    throw new FormatException("Section already present");
                m_subSections[name] = section;
            }
            else
            {
                m_subSections.Add(name, section);
            }
        }

        public T GetValue<T>(string key) where T : Value
        {
            if (!m_values.ContainsKey(key))
                throw new FormatException("Key not present");
            return (T)m_values[key];
        }

        public string GetValueOrDefault(string key, string defaultValue = "")
        {
            if (!m_values.TryGetValue(key, out Value? value))
                return defaultValue;
            return ((Values.StringValue)value).Value;
        }

        public long GetValueOrDefault(string key, long defaultValue = 0)
        {
            if (!m_values.TryGetValue(key, out Value? value))
                return defaultValue;
            return ((Values.IntValue)value).Value;
        }

        public bool GetValueOrDefault(string key, bool defaultValue = false)
        {
            if (!m_values.TryGetValue(key, out Value? value))
                return defaultValue;
            return ((Values.BooleanValue)value).Value;
        }

        public double GetValueOrDefault(string key, double defaultValue = 0.0)
        {
            if (!m_values.TryGetValue(key, out Value? value))
                return defaultValue;
            return ((Values.FloatValue)value).Value;
        }

        public List<Value>? GetValueOrDefault(string key, List<Value>? defaultValue = null)
        {
            if (!m_values.TryGetValue(key, out Value? value))
                return defaultValue;
            return ((Values.ArrayValue)value).Value;
        }

        public void SetValue(string key, Value value, bool allowOverwrite = false)
        {
            if (m_values.ContainsKey(key))
            {
                if (!allowOverwrite)
                    throw new Exception("Value already present");
                m_values[key] = value;
                return;
            }
            m_values.Add(key, value);
        }
    }

    public class Minifile
    {
        private Section m_root = new Section();

        public Section Root
        {
            get { return m_root; }
        }

        private static void WriteSection(Section section, StreamWriter sw, string partTreeName)
        {
            if (section.Values.Count > 0)
            {
                foreach (var pair in section.Values)
                {
                    if (!Utils.IsNameValid(pair.Key))
                        throw new Exception("Invalid key name");

                    foreach (var comment in pair.Value.Comments)
                        sw.WriteLine(comment);

                    sw.Write(pair.Key + " = ");
                    sw.WriteLine(pair.Value.ToString());
                }
                sw.WriteLine();
            }

            if (partTreeName.Length > 0)
                partTreeName += ".";

            foreach (var pair in section.SubSections)
            {
                if (!Utils.IsNameValid(pair.Key))
                    throw new Exception("Invalid section name");

                foreach (var comment in pair.Value.Comments)
                    sw.WriteLine(comment);

                sw.WriteLine("[" + partTreeName + pair.Key + "]");
                WriteSection(pair.Value, sw, partTreeName + pair.Key);
            }
        }

        public void Parse(string path, bool additional = false)
        {
            if (!additional || m_root == null)
                m_root = new Section();

            StreamReader sr = new StreamReader(path);
            int lineCounter = 0;
            Section? currentSection = null;

            List<string> commentBuffer = new List<string>();
            
            while (!sr.EndOfStream)
            {
                ++lineCounter;
                string? currentLine = sr.ReadLine()?.Trim();
                if (currentLine == null || currentLine.Length == 0)
                    continue;
                char firstChar = currentLine[0];
                char lastChar = currentLine[currentLine.Length - 1];

                if (firstChar == '#')
                {
                    commentBuffer.Add(currentLine);
                    continue;
                }

                if (firstChar == '[')
                {
                    if (lastChar != ']')
                        throw new FormatException("Mismatched brackets in section name");

                    string sectionPathStr = currentLine.Substring(1, currentLine.Length - 2).Trim();
                    if (sectionPathStr.Length == 0)
                        throw new FormatException("Empty section name");

                    Section ubSection = m_root;

                    string[] sectionPath = sectionPathStr.Split('.');
                    for (int i = 0; i < sectionPath.Length; ++i)
                    {
                        string sectionName = sectionPath[i];
                        if (!Utils.IsNameValid(sectionName))
                            throw new FormatException($"Invalid section name ({sectionName}) May only contain [a - z][A - Z][0 - 9] and _.");

                        Section nSection = new();
                        bool sectionOk = false;
                        try
                        {
                            ubSection.SetSubSection(sectionName, nSection, false);
                            sectionOk = true;
                        }
                        catch { }
                        if (!sectionOk && i == sectionPath.Length - 1)
                            throw new FormatException("Section already present");
                        ubSection = ubSection.SubSections[sectionName];
                    }
                    currentSection = ubSection;
                    currentSection.Comments.AddRange(commentBuffer);
                    commentBuffer.Clear();
                    continue;
                }
                if (currentSection == null)
                    throw new FormatException("Key-value pair outside of section");

                int keyValueDelimiterIndex = currentLine.IndexOf('=');
                if (keyValueDelimiterIndex == -1)
                    throw new FormatException("Invalid key-value pair");

                var keyValuePair = new KeyValuePair<string, string>(
                    currentLine.Substring(0, keyValueDelimiterIndex).Trim(),
                    currentLine.Substring(keyValueDelimiterIndex + 1).Trim());

                if (keyValuePair.Key.Length == 0)
                    throw new FormatException("Empty key");
                if (!Utils.IsNameValid(keyValuePair.Key))
                    throw new FormatException($"Invalid key name ({keyValuePair.Key}) May only contain [a - z][A - Z][0 - 9] and _.");

                if (keyValuePair.Value.Length == 0)
                    throw new FormatException("Empty value");

                var parsedValue = Value.ParseValue(keyValuePair.Value);
                if (parsedValue == null)
                {
                    throw new FormatException("Invalid value");
                }

                parsedValue.Comments.AddRange(commentBuffer);
                commentBuffer.Clear();

                currentSection.Values.Add(keyValuePair.Key, parsedValue);
            }

            sr.Close();
        }

        public void Write(string path)
        {
            StreamWriter sw = new StreamWriter(path);
            WriteSection(m_root, sw, "");
            sw.Flush();
        }

        private static void SerializeSingle(Section root, object o)
        {
            var fields = o.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            bool serializeWholeClass = o.GetType().GetCustomAttribute<MiniSerialize>() != null;

            foreach (var field in fields)
            {
                var attribute = field.GetCustomAttribute<MiniSerialize>();
                if (attribute == null && !serializeWholeClass)
                    continue;
                if (field.GetCustomAttribute<MiniDoNotSerialize>() != null)
                    continue;

                if (field.FieldType == typeof(string))
                {
                    string? value = (string?)field.GetValue(o);
                    if (value == null)
                        continue;
                    root.SetValue(attribute?.Name == null ? field.Name : attribute.Name, new StringValue(value));
                }
                else if (field.FieldType == typeof(int))
                {
                    int? value = (int?)field.GetValue(o);
                    if (value == null)
                        continue;
                    root.SetValue(attribute?.Name == null ? field.Name : attribute.Name, new IntValue(value.Value, attribute?.IntStyle));
                }
                else if (field.FieldType == typeof(long))
                {
                    long? value = (long?)field.GetValue(o);
                    if (value == null)
                        continue;
                    root.SetValue(attribute?.Name == null ? field.Name : attribute.Name, new IntValue(value.Value, attribute?.IntStyle));
                }
                else if (field.FieldType == typeof(bool))
                {
                    bool? value = (bool?)field.GetValue(o);
                    if (value == null)
                        continue;
                    root.SetValue(attribute?.Name == null ? field.Name : attribute.Name, new BooleanValue(value.Value));
                }
                else if (field.FieldType == typeof(double))
                {
                    double? value = (double?)field.GetValue(o);
                    if (value == null)
                        continue;
                    root.SetValue(attribute?.Name == null ? field.Name : attribute.Name, new FloatValue(value.Value));
                }
                else if (field.FieldType == typeof(float))
                {
                    float? value = (float?)field.GetValue(o);
                    if (value == null)
                        continue;
                    root.SetValue(attribute?.Name == null ? field.Name : attribute.Name, new FloatValue(value.Value));
                }
                else if (field.FieldType.IsInstanceOfType(typeof(List<object>)))
                {
                    List<Value>? value = (List<Value>?)field.GetValue(o);
                    if (value == null)
                        continue;
                    root.SetValue(attribute?.Name == null ? field.Name : attribute.Name, new ArrayValue());
                    foreach (var val in value)
                    {
                        ((ArrayValue)root.Values[attribute?.Name == null ? field.Name : attribute.Name]).Value.Add(val);
                    }
                }
                else
                {
                    var val = field.GetValue(o);
                    if (val == null)
                        continue;
                    Section subSection = new Section();
                    SerializeSingle(subSection, val);
                    root.SetSubSection(attribute?.Name == null ? field.Name : attribute.Name, subSection);
                }
            }
        }

        public static Minifile Serialize(object o, string rootSectionName)
        {        
            Minifile minifile = new Minifile();
            Section root = new Section();
            minifile.Root.SetSubSection(rootSectionName, root);
            SerializeSingle(root, o);

            return minifile;
        }
    }

    public static class Utils
    {
        public static bool IsNameValid(string name)
        {
            foreach (char c in name)
            {
                if (
                    (c >= 'a' && c <= 'z') ||
                    (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') ||
                    (c == '_'))
                    continue;
                return false;
            }

            return true;
        }

        public static string Replace(string str, int index, string value)
        {
            return str.Remove(index, 1).Insert(index, value);
        }
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public class MiniSerialize : Attribute
{
    public string? Name { get; set; }
    public EIntStyle IntStyle { get; set; }
    public List<string> Comments { get; set; } = new List<string>();

    public MiniSerialize(string? name = null, MiniSharp.EIntStyle intStyle = EIntStyle.Decimal)
    {
        Name = name;
        IntStyle = intStyle;
    }

    public MiniSerialize(List<string> comments, string? name = null, MiniSharp.EIntStyle intStyle = EIntStyle.Decimal)
    {
        Name = name;
        IntStyle = intStyle;
        Comments = comments;
    }
}

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public class MiniDoNotSerialize : Attribute
{

}