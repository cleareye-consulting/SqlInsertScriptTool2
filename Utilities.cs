using System.Reflection;
using System.Text.RegularExpressions;

static class Utilities
{

    private static Regex flagPattern = new(@"^--(\p{Ll}\p{L}+)$");

    public static T? GetCommandLineArgs<T>(string[] args)
    {
        if (args.Length == 0)
        {
            return default;
        }
        if (args.Length % 2 != 0)
        {
            throw new ArgumentException($"Number of args must be even. {args.Length} arguments provided.");
        }
        ConstructorInfo? ctor = typeof(T).GetConstructor(System.Type.EmptyTypes);
        if (ctor is null)
        {
            throw new InvalidOperationException("Generic type being created must have a zero-parameter constructor");
        }
        T result = (T)ctor.Invoke(new object[0]);
        int argIndex = 0;
        while (argIndex < args.Length)
        {
            string flag = args[argIndex++];
            Match flagMatch = flagPattern.Match(flag);
            if (!flagMatch.Success)
            {
                throw new ArgumentException("Odd-numbered arguments must be camel-cased identifiers prefixed with --", nameof(args));
            }
            string key = UpperCaseInitialLetter(flagMatch.Groups[1].Value);
            string valueAsString = args[argIndex++];
            PropertyInfo? prop = typeof(T).GetProperty(key);
            if (prop is null)
            {
                throw new ArgumentException($"Argument {key} doesn't have a corresponding property in type {typeof(T).Name}", nameof(args));
            }
            object? value = TryConvert(valueAsString, prop.PropertyType);
            prop.SetValue(result, value);
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

    public static string GetPasswordFromConsole()
    {
        Console.Write("Password: ");
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

    private static object? TryConvert(string input, Type type)
    {
        if (input is null)
        {
            return null;
        }
        if (type == typeof(string))
        {
            return input;
        }
        if (type == typeof(string[]))
        {
            return input.Split(',');
        }
        //Is this a hack? Yeah, probably.
        MethodInfo? parseMethod = type.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, new[] { typeof(string) });
        if (parseMethod is null)
        {
            throw new ArgumentException("Type must have a public static Parse method that accepts a string argument", nameof(type));
        }
        object? result = parseMethod.Invoke(null, new[] { input });
        return result;
    }

}