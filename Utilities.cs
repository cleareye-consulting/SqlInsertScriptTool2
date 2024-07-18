using System.Reflection;
using System.Text.RegularExpressions;

static class Utilities
{

    private static readonly Regex flagPattern = new(@"^-{1,2}(\p{Ll}\p{L}*)$");

    public static T? GetCommandLineArgs<T>(string[] args)
    {
        ConstructorInfo? ctor = typeof(T).GetConstructor(System.Type.EmptyTypes) ?? throw new InvalidOperationException("Generic type being created must have a zero-parameter constructor");
        T result = (T)ctor.Invoke(Array.Empty<object>());
        foreach (PropertyInfo prop in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            ArgumentInfoAttribute? argumentInfo = prop.GetCustomAttribute<ArgumentInfoAttribute>();
            int flagIndex = Array.IndexOf(args, $"--{LowerCaseInitialLetter(prop.Name)}");
            if (flagIndex == -1 && argumentInfo?.Alias is not null)
            {
                flagIndex = Array.IndexOf(args, $"-{argumentInfo?.Alias}");
            }
            if (flagIndex == -1)
            {
                if (argumentInfo?.IsRequired ?? false)
                {
                    if (argumentInfo?.PromptIfMissing ?? false)
                    {
                        object? inputValue = GetInputFromConsole(prop.Name, argumentInfo?.IsSecret ?? false);
                        prop.SetValue(result, inputValue);
                        continue;
                    }
                    else
                    {
                        throw new ArgumentException($"Unable to find argument for property {prop.Name}");
                    }
                }
                else
                {
                    continue;
                }
            }
            List<string> inputValues = new();
            for (int i = flagIndex + 1; i < args.Length; i++)
            {
                if (args[i][0] == '-')
                {
                    break;
                }
                inputValues.Add(args[i]);
            }
            object? argumentValue = TryConvert(inputValues.ToArray(), prop.PropertyType);
            prop.SetValue(result, argumentValue);
        }
        return result;
    }

    private static string UpperCaseInitialLetter(string camelCasedString)
    {
        char[] result = new char[camelCasedString.Length];
        if (!char.IsLower(camelCasedString[0]))
        {
            throw new ArgumentException("Expected camel-cased input");
        }
        result[0] = char.ToUpper(camelCasedString[0]);
        Array.Copy(camelCasedString.ToCharArray(), 1, result, 1, camelCasedString.Length - 1);
        return new string(result);
    }

    private static string LowerCaseInitialLetter(string input)
    {
        return char.ToLower(input[0]) + input.Substring(1);
    }

    public static string GetInputFromConsole(string prompt, bool isSecret)
    {
        Console.Write($"{prompt}: ");
        return isSecret ? ReadHiddenInputFromConsole() : (Console.ReadLine() ?? "");
    }

    public static string ReadHiddenInputFromConsole()
    {
        ConsoleKey key;
        Stack<char> inputs = new();
        do
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
            key = keyInfo.Key;
            if (key == ConsoleKey.Backspace && inputs.Any())
            {
                _ = inputs.Pop(); //discard
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                inputs.Push(keyInfo.KeyChar);
            }
        } while (key != ConsoleKey.Enter);
        Console.WriteLine();
        return new string(inputs.Reverse().ToArray());
    }

    public static string GetPasswordFromConsole()
    {
        return GetInputFromConsole("Password", true);
    }

    private static object? TryConvert(string[] inputs, Type type)
    {
        if (inputs is null)
        {
            return null;
        }
        if (inputs.Length == 0)
        {
            if (type == typeof(bool))
            {
                return true; //a boolean flag with no specified value is assumed to be true
            }
            return null;
        }
        if (type == typeof(string[]))
        {
            return inputs;
        }
        if (inputs.Length > 1)
        {
            throw new ArgumentException("Input should be a single value unless the property type is string[]", nameof(inputs));
        }
        if (type == typeof(string))
        {
            return inputs[0];
        }
        //Is this a hack? Yeah, probably.
        MethodInfo? parseMethod = type.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, new[] { typeof(string) });
        if (parseMethod is null)
        {
            throw new ArgumentException("Type must have a public static Parse method that accepts a string argument", nameof(type));
        }
        object? result = parseMethod.Invoke(null, new[] { inputs[0] });
        return result;
    }

    public static void WriteUsage<T>(TextWriter writer)
    {
        PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        writer.WriteLine("The following properties are defined:");
        foreach (PropertyInfo property in properties)
        {
            ArgumentInfoAttribute? info = property.GetCustomAttribute<ArgumentInfoAttribute>();
            writer.Write($"\t--{LowerCaseInitialLetter(property.Name)}");
            if (info is not null)
            {
                writer.Write($" (or -{info.Alias})");
            }
            writer.WriteLine();
        }
    }

    public static IEnumerable<string> SplitCSVLine(string line)
    {
        char[] buffer = new char[256];
        int bufferIndex = 0;
        List<string> results = new();
        int lineIndex = 0;
        bool isInsideQuotes = false;
        void acceptResult()
        {
            string value = new(buffer, 0, bufferIndex);
            results.Add(value);
            Array.Clear(buffer);
            bufferIndex = 0;

        }
        while (true)
        {
            char c = line[lineIndex++];
            if (bufferIndex == 0)
            {
                if (c == '"')
                {
                    isInsideQuotes = true;
                }
                else
                {
                    buffer[bufferIndex++] = c;
                }
            }
            else if (c == ',' && !isInsideQuotes)
            {
                acceptResult();
            }
            else if (c == '"' && isInsideQuotes)
            {
                acceptResult();
                lineIndex += 1;
                isInsideQuotes = false;
            }
            else
            {
                buffer[bufferIndex++] = c;
            }
            if (lineIndex == line.Length)
            {
                acceptResult();
                break;
            }
        }
        return results;
    }

}