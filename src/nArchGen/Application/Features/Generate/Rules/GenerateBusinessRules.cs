using Application.Features.Generate.Constants;
using Core.CodeGen.File;
using Core.CrossCuttingConcerns.Exceptions;
using Domain.Constants;
using System.Text.RegularExpressions;

namespace Application.Features.Generate.Rules;

public class GenerateBusinessRules
{
    public async Task EntityClassShouldBeInheritEntityBaseClass(
             string projectPath,
             string entityName
         )
    {
        string entityFilePath = Path.Combine(projectPath, $"{entityName}.cs");
        string[] fileContent = await File.ReadAllLinesAsync(entityFilePath);

        string entityBaseClassNameSpaceUsingTemplate = await File.ReadAllTextAsync(
            Path.Combine(DirectoryHelper.AssemblyDirectory, Templates.Paths.Crud, "Lines", "EntityBaseClassNameSpaceUsing.cs.sbn")
        );
        Regex entityBaseClassRegex = new($"public\\s+class\\s+{entityName}\\s*:\\s*Entity\\s*");
        bool isExists =
            fileContent.Any(line => line.Trim() == entityBaseClassNameSpaceUsingTemplate.Trim())
            && fileContent.Any(entityBaseClassRegex.IsMatch);

        if (!isExists)
            throw new BusinessException(
                GenerateBusinessMessages.EntityClassShouldBeInheritEntityBaseClass(entityName)
            );
    }

    public Task FileShouldNotBeExists(string filePath)
    {
        if (File.Exists(filePath))
            throw new BusinessException(GenerateBusinessMessages.FileAlreadyExists(filePath));
        return Task.CompletedTask;
    }
}
