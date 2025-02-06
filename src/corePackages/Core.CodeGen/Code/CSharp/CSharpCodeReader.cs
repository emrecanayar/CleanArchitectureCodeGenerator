using Core.CodeGen.Code.CSharp.ValueObjects;
using Spectre.Console;
using System.Text.RegularExpressions;

namespace Core.CodeGen.Code.CSharp;

public static class CSharpCodeReader
{
    public static async Task<string> ReadClassNameAsync(string filePath)
    {
        string fileContent = await System.IO.File.ReadAllTextAsync(filePath);
        const string pattern = @"class\s+(\w+)";

        Match match = Regex.Match(fileContent, pattern);
        if (!match.Success)
            return string.Empty;

        return match.Groups[1].Value;
    }

    public static async Task<string> ReadBaseClassNameAsync(string filePath)
    {
        string fileContent = await System.IO.File.ReadAllTextAsync(filePath);
        const string pattern = @"class\s+\w+\s*:?\s*(\w+)";

        Match match = Regex.Match(fileContent, pattern);
        if (!match.Success)
            return string.Empty;

        return match.Groups[1].Value;
    }

    public static async Task<ICollection<string>> ReadBaseClassGenericArgumentsAsync(
        string filePath
    )
    {
        string fileContent = await System.IO.File.ReadAllTextAsync(filePath);
        const string pattern = @"class\s+\w+\s*:?\s*(\w+)\s*<([\w,\s]+)>";

        Match match = Regex.Match(fileContent, pattern);
        if (!match.Success)
            return new List<string>();
        string[] genericArguments = match.Groups[2].Value.Split(',');

        return genericArguments.Select(genericArgument => genericArgument.Trim()).ToArray();
    }

    public static async Task<ICollection<PropertyInfo>> ReadClassPropertiesAsync(string filePath, string projectPath)
    {
        string fileContent = await System.IO.File.ReadAllTextAsync(filePath);

        Regex propertyRegex = new Regex(@"(\bpublic\b|\bprotected\b|\binternal\b|\bprivate\b)\s+(static\s+)?([\w\?]+)\s+(\w+)\s*{", RegexOptions.Compiled | RegexOptions.Singleline);

        Regex builtInTypeRegex = new Regex(@"^(bool\??|byte\??|sbyte\??|char\??|decimal\??|double\??|float\??|int\??|uint\??|long\??|ulong\??|object\??|short\??|ushort\??|string\??|DateTime\??|Guid\??)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        List<PropertyInfo> result = new List<PropertyInfo>();

        HashSet<string> modelClassNames = GetModelClassNames(projectPath);

        foreach (Match match in propertyRegex.Matches(fileContent))
        {
            string accessModifier = match.Groups[1].Value;
            string typeName = match.Groups[3].Value;
            string propertyName = match.Groups[4].Value;

            //string message = $"accessModifier:{accessModifier}, typeName:{typeName}, propertyName:{propertyName} ";

            //AnsiConsole.MarkupLine(message);

            if (builtInTypeRegex.IsMatch(typeName))
            {
                AddProperty(result, accessModifier, typeName, propertyName);
                continue;
            }
            if (modelClassNames.Contains(typeName))
            {
                continue;
            }
            AddProperty(result, accessModifier, typeName, propertyName);
        }

        return result;
    }
    static void AddProperty(List<PropertyInfo> list, string accessModifier, string typeName, string propertyName)
    {
        list.Add(new PropertyInfo
        {
            AccessModifier = accessModifier,
            Type = typeName,
            Name = propertyName,
            NameSpace = null
        });
    }

    static HashSet<string> GetModelClassNames(string directoryPath)
    {
        HashSet<string> classNames = new HashSet<string>();

        if (Directory.Exists(directoryPath))
        {
            var files = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var lines = System.IO.File.ReadAllLines(file);
                foreach (var line in lines)
                {
                    var match = Regex.Match(line, @"\bclass\s+(\w+)");
                    if (match.Success)
                    {
                        classNames.Add(match.Groups[1].Value);
                    }
                }
            }
        }

        return classNames;
    }

    public static async Task<ICollection<string>> ReadUsingNameSpacesAsync(string filePath)
    {
        ICollection<string> fileContent = await System.IO.File.ReadAllLinesAsync(filePath);
        Regex usingRegex = new("^using\\s+(.+);");

        ICollection<string> usingNameSpaces = fileContent
            .Where(line => usingRegex.IsMatch(line))
            .Select(usingNameSpace => usingRegex.Match(usingNameSpace).Groups[1].Value)
            .ToList();

        return usingNameSpaces;
    }
}
