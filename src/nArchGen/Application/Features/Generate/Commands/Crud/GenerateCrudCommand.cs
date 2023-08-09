using Application.Features.Generate.Rules;
using Core.CodeGen.Code.CSharp;
using Core.CodeGen.File;
using Core.CodeGen.TemplateEngine;
using Domain.Constants;
using Domain.ValueObjects;
using MediatR;
using System.Runtime.CompilerServices;

namespace Application.Features.Generate.Commands.Crud;

public class GenerateCrudCommand : IStreamRequest<GeneratedCrudResponse>
{
    public string ProjectPath { get; set; }
    public string ProjectName { get; set; }
    public CrudTemplateData CrudTemplateData { get; set; }
    public string DbContextName { get; set; }

    public class GenerateCrudCommandHandler
        : IStreamRequestHandler<GenerateCrudCommand, GeneratedCrudResponse>
    {
        private readonly ITemplateEngine _templateEngine;
        private readonly GenerateBusinessRules _businessRules;

        public GenerateCrudCommandHandler(
            ITemplateEngine templateEngine,
            GenerateBusinessRules businessRules
        )
        {
            _templateEngine = templateEngine;
            _businessRules = businessRules;
        }

        public async IAsyncEnumerable<GeneratedCrudResponse> Handle(
            GenerateCrudCommand request,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            await _businessRules.EntityClassShouldBeInhreitEntityBaseClass(
                request.ProjectPath,
                request.CrudTemplateData.Entity.Name
            );

            GeneratedCrudResponse response = new();
            List<string> newFilePaths = new();
            List<string> updatedFilePaths = new();

            response.CurrentStatusMessage =
                $"Adding {request.CrudTemplateData.Entity.Name} entity to BaseContext.";
            yield return response;

            updatedFilePaths.Add(
                await injectEntityToContext(request.ProjectPath, request.CrudTemplateData)
            );
            response.LastOperationMessage =
                $"{request.CrudTemplateData.Entity.Name} has been added to BaseContext.";
            yield return response;

            response.CurrentStatusMessage = "Generating Persistence layer codes...";
            yield return response;
            newFilePaths.AddRange(
                await generatePersistenceCodes(request.ProjectPath, request.ProjectName, request.CrudTemplateData)
            );
            response.LastOperationMessage = "Persistence layer codes have been generated.";

            response.CurrentStatusMessage = "Generating Application layer codes...";
            yield return response;

            newFilePaths.AddRange(
                await generateApplicationCodes(request.ProjectPath, request.ProjectName, request.CrudTemplateData)
            );
            response.LastOperationMessage = "Application layer codes have been generated.";
            yield return response;

            response.CurrentStatusMessage = "Adding service registrations...";
            yield return response;

            response.CurrentStatusMessage = "Generating WebAPI layer codes...";
            yield return response;
            newFilePaths.AddRange(
                await generateWebApiCodes(request.ProjectPath, request.ProjectName, request.CrudTemplateData)
            );
            response.LastOperationMessage = "WebAPI layer codes have been generated.";
            yield return response;

            response.CurrentStatusMessage = "Completed.";
            response.NewFilePathsResult = newFilePaths;
            response.UpdatedFilePathsResult = updatedFilePaths;
            yield return response;
        }

        private async Task<string> injectEntityToContext(
            string projectPath,
            CrudTemplateData crudTemplateData
        )
        {
            string persistencePath = $@"{Environment.CurrentDirectory}\src\corePackages\Core.Persistence\Contexts\{crudTemplateData.DbContextName}.cs";

            string[] entityNameSpaceUsingTemplate = await File.ReadAllLinesAsync(
                @$"{DirectoryHelper.AssemblyDirectory}\{Templates.Paths.Crud}\Lines\EntityNameSpaceUsing.cs.sbn"
            );
            await CSharpCodeInjector.AddUsingToFile(persistencePath, entityNameSpaceUsingTemplate);

            return persistencePath;
        }

        private async Task<ICollection<string>> generatePersistenceCodes(
        string projectPath,
        string projectName,
        CrudTemplateData crudTemplateData
    )
        {
            string templateConfigurationDir =
                @$"{DirectoryHelper.AssemblyDirectory}\{Templates.Paths.Crud}\Folders\Persistence\EntityConfigurations";
            string templateRepositoryDir =
                @$"{DirectoryHelper.AssemblyDirectory}\{Templates.Paths.Crud}\Folders\Persistence\Repositories";

            string persistencePath =
                $@"{Environment.CurrentDirectory}\src\corePackages\Core.Persistence\Configurations";

            projectPath =
                projectPath.Replace("corePackages", "projects").Replace("Core.Domain\\Entities", projectName);

            await generateFolderCodes(
               templateConfigurationDir,
               outputDir: $@"{persistencePath}",
               crudTemplateData);

            return await generateFolderCodes(
              templateRepositoryDir,
              outputDir: $@"{projectPath}\webAPI.Persistence\Repositories",
              crudTemplateData);
        }

        private async Task<ICollection<string>> generateApplicationCodes(
            string projectPath,
            string projectName,
            CrudTemplateData crudTemplateData
        )
        {

            string templateDir =
                @$"{DirectoryHelper.AssemblyDirectory}\{Templates.Paths.Crud}\Folders\Application";

            projectPath = projectPath.Replace("corePackages", "projects").Replace("Core.Domain\\Entities", projectName);


            return await generateFolderCodes(
                templateDir,
                outputDir: $@"{projectPath}\webAPI.Application",
                crudTemplateData
            );
        }


        private async Task<ICollection<string>> generateWebApiCodes(
            string projectPath,
            string projectName,
            CrudTemplateData crudTemplateData
        )
        {
            string templateDir =
                @$"{DirectoryHelper.AssemblyDirectory}\{Templates.Paths.Crud}\Folders\WebAPI";

            projectPath = projectPath.Replace("corePackages", "projects").Replace("Core.Domain\\Entities", projectName);

            return await generateFolderCodes(
                templateDir,
                outputDir: $@"{projectPath}\webAPI",
                crudTemplateData
            );
        }

        private async Task<ICollection<string>> generateFolderCodes(
            string templateDir,
            string outputDir,
            CrudTemplateData crudTemplateData
        )
        {
            var templateFilePaths = DirectoryHelper
                .GetFilesInDirectoryTree(
                    templateDir,
                    searchPattern: $"*.{_templateEngine.TemplateExtension}"
                )
                .ToList();
            Dictionary<string, string> replacePathVariable =
                new()
                {
                    { "PLURAL_ENTITY", "{{ entity.name | string.pascalcase | string.plural }}" },
                    { "ENTITY", "{{ entity.name | string.pascalcase }}" }
                };
            ICollection<string> newRenderedFilePaths = await _templateEngine.RenderFileAsync(
                templateFilePaths,
                templateDir,
                replacePathVariable,
                outputDir,
                crudTemplateData
            );
            return newRenderedFilePaths;
        }

    }
}
