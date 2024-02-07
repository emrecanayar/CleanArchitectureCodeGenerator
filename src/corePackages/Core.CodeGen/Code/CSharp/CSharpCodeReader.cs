using Core.CodeGen.Code.CSharp.ValueObjects;
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

    public static async Task<ICollection<PropertyInfo>> ReadClassPropertiesAsync(
     string filePath,
     string projectPath
 )
    {
        string fileContent = await System.IO.File.ReadAllTextAsync(filePath);
        Regex propertyRegex = new(
            @"(public|protected|internal|protected internal|private protected|private)?\s+(?:const|static)?\s*((?:\w+\.?)+\??)\s+(\w+)\s*\{.*\}"
        );
        Regex builtInTypeRegex = new(
            @"^(bool|byte|sbyte|char|decimal|double|float|int|uint|long|ulong|object|short|ushort|string|DateTime|Guid)$",
            RegexOptions.IgnoreCase
        );

        MatchCollection matches = propertyRegex.Matches(fileContent);
        List<PropertyInfo> result = new();
        foreach (Match match in matches)
        {
            string type = match.Groups[2].Value;
            string typeName = type.Replace("?", string.Empty);

            if (!builtInTypeRegex.IsMatch(typeName))
            {
                continue;
            }

            string accessModifier = match.Groups[1].Value.Trim();
            string name = match.Groups[3].Value;

            PropertyInfo propertyInfo = new()
            {
                AccessModifier = string.IsNullOrEmpty(accessModifier) ? "private" : accessModifier,
                Type = type,
                Name = name,
                NameSpace = null
            };
            result.Add(propertyInfo);
        }

        return result;
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
