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
    public string ProjectPath { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public CrudTemplateData CrudTemplateData { get; set; } = default!;
    public string DbContextName { get; set; } = string.Empty;

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
            bool isAdminProject = request.ProjectName.Contains("Admin");

            await _businessRules.EntityClassShouldBeInheritEntityBaseClass(
                request.ProjectPath,
                request.CrudTemplateData.Entity.Name
            );

            GeneratedCrudResponse response = new();
            List<string> newFilePaths = new();
            List<string> updatedFilePaths = new();


            if (!isAdminProject)
            {
                response.CurrentStatusMessage =
                $"Adding {request.CrudTemplateData.Entity.Name} entity to BaseContext.";
                yield return response;
                updatedFilePaths.Add(
               await injectEntityToContext(request.ProjectPath, request.CrudTemplateData));
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
            else
            {
                response.CurrentStatusMessage = "Generating Persistence layer codes...";
                yield return response;
                newFilePaths.AddRange(
                    await generateAdminPersistenceCodes(request.ProjectPath, request.ProjectName, request.CrudTemplateData)
                );
                response.LastOperationMessage = "Persistence layer codes have been generated.";

                response.CurrentStatusMessage = "Generating Application layer codes...";
                yield return response;

                newFilePaths.AddRange(
                    await generateAdminApplicationCodes(request.ProjectPath, request.ProjectName, request.CrudTemplateData)
                );
                response.LastOperationMessage = "Application layer codes have been generated.";
                yield return response;

                response.CurrentStatusMessage = "Adding service registrations...";
                yield return response;

                response.CurrentStatusMessage = "Generating WebAPI layer codes...";
                yield return response;
                newFilePaths.AddRange(
                    await generateAdminWebApiCodes(request.ProjectPath, request.ProjectName, request.CrudTemplateData)
                );
                response.LastOperationMessage = "WebAPI layer codes have been generated.";
                yield return response;

                response.CurrentStatusMessage = "Completed.";
                response.NewFilePathsResult = newFilePaths;
                response.UpdatedFilePathsResult = updatedFilePaths;
                yield return response;
            }



        }

        private async Task<string> injectEntityToContext(
               string projectPath,
               CrudTemplateData crudTemplateData)
        {
            string persistencePath = Path.Combine(Environment.CurrentDirectory, "src", "corePackages", "Core.Persistence", "Contexts", $"{crudTemplateData.DbContextName}.cs");

            string[] entityNameSpaceUsingTemplate = await File.ReadAllLinesAsync(
                Path.Combine(DirectoryHelper.AssemblyDirectory, Templates.Paths.Crud, "Lines", "EntityNameSpaceUsing.cs.sbn")
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
                Path.Combine(DirectoryHelper.AssemblyDirectory, Templates.Paths.Crud, "Folders", "Persistence", "EntityConfigurations");
            string templateRepositoryDir =
                Path.Combine(DirectoryHelper.AssemblyDirectory, Templates.Paths.Crud, "Folders", "Persistence", "Repositories");

            string persistencePath =
                Path.Combine(Environment.CurrentDirectory, "src", "corePackages", "Core.Persistence", "Configurations");

            projectPath =
                projectPath.Replace("corePackages", "projects").Replace(Path.Combine("Core.Domain", "Entities"), projectName);

            await generateFolderCodes(
               templateConfigurationDir,
               outputDir: persistencePath,
               crudTemplateData);

            return await generateFolderCodes(
              templateRepositoryDir,
              outputDir: Path.Combine(projectPath, "webAPI.Persistence", "Repositories"),
              crudTemplateData);
        }

        private async Task<ICollection<string>> generateAdminPersistenceCodes(
                  string projectPath,
                  string projectName,
                  CrudTemplateData crudTemplateData)
        {
            string templateRepositoryDir =
                Path.Combine(DirectoryHelper.AssemblyDirectory, Templates.Paths.Crud, "Folders", "Persistence", "Repositories");

            projectPath =
                projectPath.Replace("corePackages", "projects").Replace(Path.Combine("Core.Domain", "Entities"), projectName);

            return await generateFolderCodes(
                templateRepositoryDir,
                outputDir: Path.Combine(projectPath, "webAPI.Persistence", "Repositories"),
                crudTemplateData);
        }

        private async Task<ICollection<string>> generateApplicationCodes(
          string projectPath,
          string projectName,
          CrudTemplateData crudTemplateData
      )
        {
            try
            {
                  string templateDir = Path.Combine(DirectoryHelper.AssemblyDirectory, Templates.Paths.Crud, "Folders", "Application");

            projectPath = projectPath.Replace("corePackages", "projects")
                                     .Replace(Path.Combine("Core.Domain", "Entities"), projectName);

            return await generateFolderCodes(
                templateDir,
                outputDir: Path.Combine(projectPath, "webAPI.Application"),
                crudTemplateData
            );
            }
            catch (Exception exception)
            {
                System.Console.WriteLine("Error: " + exception.Message);
                System.Console.WriteLine("Error Stack Trace: " + exception.StackTrace);
                System.Console.WriteLine("Error Inner Exception: " + exception.InnerException);
                System.Console.WriteLine("Error Source: " + exception.Source);
                System.Console.WriteLine("Error Target Site: " + exception.TargetSite);
                System.Console.WriteLine("Error Data: " + exception.Data);
                System.Console.WriteLine("Error Help Link: " + exception.HelpLink); 
                System.Console.WriteLine("Error HResult: " + exception.HResult);
                throw;
            }
          
        }

        private async Task<ICollection<string>> generateAdminApplicationCodes(
             string projectPath,
             string projectName,
             CrudTemplateData crudTemplateData
         )
        {
            string templateDir =
                Path.Combine(DirectoryHelper.AssemblyDirectory, Templates.Paths.Crud, "Folders", "Application");

            projectPath = projectPath.Replace("corePackages", "projects").Replace(Path.Combine("Core.Domain", "Entities"), projectName);

            return await generateFolderCodes(
                templateDir,
                outputDir: Path.Combine(projectPath, "webAPI.Application"),
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
                Path.Combine(DirectoryHelper.AssemblyDirectory, Templates.Paths.Crud, "Folders", "WebAPI");

            projectPath = projectPath.Replace("corePackages", "projects").Replace(Path.Combine("Core.Domain", "Entities"), projectName);

            return await generateFolderCodes(
                templateDir,
                outputDir: Path.Combine(projectPath, "webAPI"),
                crudTemplateData
            );
        }

        private async Task<ICollection<string>> generateAdminWebApiCodes(
                string projectPath,
                string projectName,
                CrudTemplateData crudTemplateData
            )
        {
            string templateDir =
                Path.Combine(DirectoryHelper.AssemblyDirectory, Templates.Paths.Crud, "Folders", "WebAPI");

            projectPath = projectPath.Replace("corePackages", "projects").Replace(Path.Combine("Core.Domain", "Entities"), projectName);

            return await generateFolderCodes(
                templateDir,
                outputDir: Path.Combine(projectPath, "webAPI"),
                crudTemplateData
            );
        }


        private async Task<ICollection<string>> generateFolderCodes(
               string templateDir,
               string outputDir,
               CrudTemplateData crudTemplateData
           )
        {

            var templateFilePaths = Directory.GetFiles(templateDir, "*.sbn", SearchOption.AllDirectories).ToList();

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

        private async Task<ICollection<string>> generateAdminFolderCodes(
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
