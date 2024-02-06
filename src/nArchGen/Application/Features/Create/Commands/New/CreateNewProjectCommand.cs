using Core.CodeGen.Code;
using Core.CodeGen.CommandLine.Git;
using Core.CodeGen.File;
using MediatR;
using System.Runtime.CompilerServices;

namespace Application.Features.Create.Commands.New;

public class CreateNewProjectCommand : IStreamRequest<CreatedNewProjectResponse>
{
    public string ProjectName { get; set; }
    public bool IsThereSecurityMechanism { get; set; } = true;
    public bool IsThereAdminProject { get; set; } = true;

    public CreateNewProjectCommand()
    {
        ProjectName = string.Empty;
    }

    public CreateNewProjectCommand(string projectName, bool isThereSecurityMechanism, bool isThereAdminProject)
    {
        ProjectName = projectName;
        IsThereSecurityMechanism = isThereSecurityMechanism;
        IsThereAdminProject = isThereAdminProject;
    }

    public class CreateNewProjectCommandHandler
        : IStreamRequestHandler<CreateNewProjectCommand, CreatedNewProjectResponse>
    {
        public async IAsyncEnumerable<CreatedNewProjectResponse> Handle(
            CreateNewProjectCommand request,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            CreatedNewProjectResponse response = new();
            List<string> newFilePaths = new();

            response.CurrentStatusMessage = "Cloning starter project and core packages...";
            yield return response;
            response.OutputMessage = null;
            await cloneCorePackagesAndStarterProject(request.ProjectName);
            response.LastOperationMessage =
                "Starter project has been cloned from 'https://github.com/emrecanayar/CleanArchitectureTemplate.git'.";

            response.CurrentStatusMessage = "Preparing project...";
            yield return response;
            await renameProject(request.ProjectName);

            if (request.IsThereAdminProject)
            {
                await renameForAdminProject($"{request.ProjectName}");
            }
            else
            {
                await deleteForAdminProject($"{request.ProjectName}");
            }

            if (!request.IsThereSecurityMechanism)
            {
                await removeSecurityMechanism(request.ProjectName);
                if (request.IsThereAdminProject)
                {
                    await removeSecurityMechanismForAdminProject(request.ProjectName);
                }
            }




            response.LastOperationMessage =
                $"Project has been prepared with {request.ProjectName.ToPascalCase()}.";

            DirectoryHelper.DeleteDirectory(
                $"{Environment.CurrentDirectory}/{request.ProjectName}/.git"
            );
            ICollection<string> newFiles = DirectoryHelper.GetFilesInDirectoryTree(
                root: $"{Environment.CurrentDirectory}/{request.ProjectName}",
                searchPattern: "*"
            );

            response.CurrentStatusMessage = "Initializing git repository with submodules...";
            yield return response;
            response.LastOperationMessage = "Git repository has been initialized.";

            response.CurrentStatusMessage = "Completed.";
            response.NewFilePathsResult = newFiles;
            response.OutputMessage =
                $":warning:Check the configuration that has name 'appsettings.json' in 'src/{request.ProjectName.ToCamelCase()}'. \n:warning:First identify the Startup of the project, for example webAPI \n:warning:Create an Add-Migration based on the Core Persistence layer \n:warning:Run 'Update-Database' nuget command on the Core Persistence layer to apply initial migration.";
            yield return response;
        }

        private async Task cloneCorePackagesAndStarterProject(string projectName) =>
            await GitCommandHelper.RunAsync(
                $"clone https://github.com/emrecanayar/CleanArchitectureTemplate.git ./{projectName}"
            );

        private async Task renameProject(string projectName)
        {
            Directory.SetCurrentDirectory($"./{projectName}");

            await replaceFileContentWithProjectName(
                path: $"{Environment.CurrentDirectory}/CleanArchitectureTemplate.sln",
                search: "CleanArchitectureTemplate",
                projectName: projectName.ToPascalCase()
            );
            await replaceFileContentWithProjectName(
                path: $"{Environment.CurrentDirectory}/CleanArchitectureTemplate.sln.DotSettings",
                search: "CleanArchitectureTemplate",
                projectName: projectName.ToPascalCase()
            );

            string projectPath = $"{Environment.CurrentDirectory}/src/projects/{projectName.ToCamelCase()}";
            Directory.Move(
                sourceDirName: $"{Environment.CurrentDirectory}/src/projects/starterProject",
                projectPath
            );

            await replaceFileContentWithProjectName(
                path: $"{Environment.CurrentDirectory}/{projectName.ToPascalCase()}.sln",
                search: "starterProject",
                projectName: projectName.ToCamelCase()
            );

            await replaceFileContentWithProjectName(
                path: $"{Environment.CurrentDirectory}/tests/Application.Tests/Application.Tests.csproj",
                search: "starterProject",
                projectName: projectName.ToCamelCase()
            );

            await replaceFileContentWithProjectName(
                path: $"{projectPath}/webAPI/appsettings.json",
                search: "StarterProject",
                projectName: projectName.ToPascalCase()
            );
            await replaceFileContentWithProjectName(
                path: $"{projectPath}/webAPI/appsettings.json",
                search: "starterProject",
                projectName: projectName.ToCamelCase()
            );

            Directory.SetCurrentDirectory("../");

            static async Task replaceFileContentWithProjectName(
                string path,
                string search,
                string projectName
            )
            {
                if (path.Contains(search))
                {
                    string newPath = path.Replace(search, projectName);
                    Directory.Move(path, newPath);
                    path = newPath;
                }

                string fileContent = await File.ReadAllTextAsync(path);
                fileContent = fileContent.Replace(search, projectName);
                await File.WriteAllTextAsync(path, fileContent);
            }
        }
        private async Task renameForAdminProject(string projectName)
        {

            Directory.SetCurrentDirectory($"./{projectName}");
            string projectPath = $"{Environment.CurrentDirectory}/src/projects/{projectName}AdminProject";
            Directory.Move(
                sourceDirName: $"{Environment.CurrentDirectory}/src/projects/adminProject",
                projectPath
            );

            await replaceFileContentWithProjectName(
                path: $"{Environment.CurrentDirectory}/{projectName.ToPascalCase()}.sln",
                search: "adminProject",
                projectName: $"{projectName}AdminProject"
            );

            Directory.SetCurrentDirectory("../");

            static async Task replaceFileContentWithProjectName(
                string path,
                string search,
                string projectName
            )
            {
                if (path.Contains(search))
                {
                    string newPath = path.Replace(search, projectName);
                    Directory.Move(path, newPath);
                    path = newPath;
                }

                string fileContent = await File.ReadAllTextAsync(path);
                fileContent = fileContent.Replace(search, projectName);
                await File.WriteAllTextAsync(path, fileContent);
            }


        }
        private static async Task deleteForAdminProject(string projectName)
        {
            Directory.SetCurrentDirectory($"./{projectName}");
            string solutionPath = $"{Environment.CurrentDirectory}/{projectName.ToPascalCase()}.sln";
            string directoryToDelete = $"{Environment.CurrentDirectory}/src/projects/adminProject";
            if (Directory.Exists(directoryToDelete))
            {
                Directory.Delete(directoryToDelete, true);
            }

            await RemoveProjectsFromSolutionAsync(solutionPath, new List<string> { "{8F30C7ED-8A79-4BD2-8413-CB62688F1636}", "{CF600D2A-BC95-4C87-9908-DA3F37DA5BE8}", "{1D50F0A7-8B27-44BD-97E1-B1C696D3E834}", "{3366F4DF-22C7-4542-BA19-6C7653F73C5D}", "{A120FD32-EB2D-4F6D-9AB7-8736B612DC9A}" });
            Directory.SetCurrentDirectory("../");
        }
        private async Task removeSecurityMechanism(string projectName)
        {
            string slnPath = $"{Environment.CurrentDirectory}/{projectName.ToPascalCase()}";
            string projectSourcePath = $"{slnPath}/src/projects/{projectName.ToCamelCase()}";
            string corePackagePath = $"{slnPath}/src/corePackages";
            string projectTestsPath = $"{slnPath}/tests/";

            string[] dirsToDelete = new[]
            {
                $"{projectSourcePath}/webAPI.Application/Features/Auth",
                $"{projectSourcePath}/webAPI.Application/Features/OperationClaims",
                $"{projectSourcePath}/webAPI.Application/Features/UserOperationClaims",
                $"{projectSourcePath}/webAPI.Application/Features/Users",
                $"{projectSourcePath}/webAPI.Application/Services/AuthenticatorService",
                $"{projectSourcePath}/webAPI.Application/Services/AuthService",
                $"{projectSourcePath}/webAPI.Application/Services/OperationClaims",
                $"{projectSourcePath}/webAPI.Application/Services/UserOperationClaims",
                $"{projectSourcePath}/webAPI.Application/Services/UsersService",
                $"{projectTestsPath}/Application.Tests/Features/Users",
            };
            foreach (string dirPath in dirsToDelete)
                Directory.Delete(dirPath, recursive: true);

            string[] filesToDelete = new[]
            {
                $"{projectSourcePath}/webAPI.Application/Services/Repositories/IEmailAuthenticatorRepository.cs",
                $"{projectSourcePath}/webAPI.Application/Services/Repositories/IOperationClaimRepository.cs",
                $"{projectSourcePath}/webAPI.Application/Services/Repositories/IOtpAuthenticatorRepository.cs",
                $"{projectSourcePath}/webAPI.Application/Services/Repositories/IRefreshTokenRepository.cs",
                $"{projectSourcePath}/webAPI.Application/Services/Repositories/IUserOperationClaimRepository.cs",
                $"{projectSourcePath}/webAPI.Application/Services/Repositories/IUserRepository.cs",
                $"{corePackagePath}/Core.Persistence/Configurations/EmailAuthenticatorConfiguration.cs",
                $"{corePackagePath}/Core.Persistence/Configurations/OperationClaimConfiguration.cs",
                $"{corePackagePath}/Core.Persistence/Configurations/OtpAuthenticatorConfiguration.cs",
                $"{corePackagePath}/Core.Persistence/Configurations/RefreshTokenConfiguration.cs",
                $"{corePackagePath}/Core.Persistence/Configurations/UserConfiguration.cs",
                $"{corePackagePath}/Core.Persistence/Configurations/UserOperationClaimConfiguration.cs",
                $"{projectSourcePath}/webAPI.Persistence/Repositories/EmailAuthenticatorRepository.cs",
                $"{projectSourcePath}/webAPI.Persistence/Repositories/OperationClaimRepository.cs",
                $"{projectSourcePath}/webAPI.Persistence/Repositories/OtpAuthenticatorRepository.cs",
                $"{projectSourcePath}/webAPI.Persistence/Repositories/RefreshTokenRepository.cs",
                $"{projectSourcePath}/webAPI.Persistence/Repositories/UserOperationClaimRepository.cs",
                $"{projectSourcePath}/webAPI.Persistence/Repositories/UserRepository.cs",
                $"{projectSourcePath}/webAPI/Controllers/AuthController.cs",
                $"{projectSourcePath}/webAPI/Controllers/OperationClaimsController.cs",
                $"{projectSourcePath}/webAPI/Controllers/UserOperationClaimsController.cs",
                $"{projectSourcePath}/webAPI/Controllers/UsersController.cs",
                $"{projectTestsPath}/Application.Tests/DependencyResolvers/UsersTestServiceRegistration.cs",
                $"{projectTestsPath}/Application.Tests/Mocks/FakeData/UserFakeData.cs",
                $"{projectTestsPath}/Application.Tests/Mocks/Repositories/UserMockRepository.cs",
            };
            foreach (string filePath in filesToDelete)
                File.Delete(filePath);

            await FileHelper.RemoveLinesAsync(
              filePath: $"{projectSourcePath}/webAPI.Application/ApplicationServiceRegistration.cs",
              predicate: line =>
                  (
                      new[]
                      {
                            "using Application.Services.AuthService;",
                            "services.AddScopedWithManagers(typeof(IAuthService).Assembly);",
                      }
                  ).Any(line.Contains)
          );

            await FileHelper.RemoveLinesAsync(
                filePath: $"{projectTestsPath}/Application.Tests/Startup.cs",
                predicate: line =>
                    (
                        new[]
                        {
                            "using Application.Tests.DependencyResolvers;",
                            "public void ConfigureServices(IServiceCollection services) => services.AddUsersServices();",
                        }
                    ).Any(line.Contains)
            );

            await FileHelper.RemoveContentAsync(
                filePath: $"{projectSourcePath}/webAPI/Program.cs",
                contents: new[]
                {
                    "using Core.Security;",
                    "using Core.Security.Encryption;",
                    "using Core.Security.JWT;",
                    "using Core.WebAPI.Extensions.Swagger;",
                    "using Microsoft.AspNetCore.Authentication.JwtBearer;",
                    "using Microsoft.IdentityModel.Tokens;",
                    "using Microsoft.OpenApi.Models;",
                    "using System.Reflection;",
                    "using Swashbuckle.AspNetCore.SwaggerGen;",
                    "using Core.Utilities.ApiDoc;",
                    "using Core.Utilities.Messages;",
                    "builder.Services.AddSecurityServices();",
                    @"TokenOptions? tokenOptions = builder.Configuration.GetSection(""TokenOptions"").Get<TokenOptions>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidIssuer = tokenOptions.Issuer,
        ValidAudience = tokenOptions.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = SecurityKeyHelper.CreateSecurityKey(tokenOptions.SecurityKey)
    };
});

builder.Services.AddSwaggerGen(opt =>
{
    opt.CustomSchemaIds(type => type.ToString());
    opt.SwaggerDoc(ProjectSwaggerMessages.Version, new OpenApiInfo
    {
        Version = ProjectSwaggerMessages.Version,
        Title = ProjectSwaggerMessages.Title,
        Description = ProjectSwaggerMessages.Description
    });
    opt.CustomOperationIds(apiDesc =>
    {
        return apiDesc.TryGetMethodInfo(out MethodInfo methodInfo)
            ? $""{methodInfo.DeclaringType.Name}.{methodInfo.Name}""
            : new Guid().ToString();
    });
    opt.AddSecurityDefinition(""Bearer"", new OpenApiSecurityScheme
    {
        Name = ""Authorization"",
        Type = SecuritySchemeType.ApiKey,
        Scheme = ""Bearer"",
        BearerFormat = ""JWT"",
        In = ParameterLocation.Header,
        Description =
            ""JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \""Bearer 12345.54321\""""
    });

    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
                { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = ""Bearer"" } },
            new string[] { }
        }
    });
    opt.OperationFilter<AddAuthHeaderOperationFilter>();
    var xmlFile = $""{Assembly.GetExecutingAssembly().GetName().Name}.xml"";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    opt.IncludeXmlComments(xmlPath);
});",
@"app.UseAuthentication();
app.UseAuthorization();"
                }
            );
        }
        private async Task removeSecurityMechanismForAdminProject(string projectName)
        {
            string slnPath = $"{Environment.CurrentDirectory}/{projectName.ToPascalCase()}";

            string projectSourcePath = $"{slnPath}/src/projects/{projectName.ToCamelCase()}AdminProject";
            string corePackagePath = $"{slnPath}/src/corePackages";

            string[] dirsToDelete = new[]
            {
                $"{projectSourcePath}/webAPI.Application/Features/Auth",
                $"{projectSourcePath}/webAPI.Application/Features/OperationClaims",
                $"{projectSourcePath}/webAPI.Application/Features/UserOperationClaims",
                $"{projectSourcePath}/webAPI.Application/Features/Users",
                $"{projectSourcePath}/webAPI.Application/Services/AuthenticatorService",
                $"{projectSourcePath}/webAPI.Application/Services/AuthService",
                $"{projectSourcePath}/webAPI.Application/Services/OperationClaims",
                $"{projectSourcePath}/webAPI.Application/Services/UserOperationClaims",
                $"{projectSourcePath}/webAPI.Application/Services/UsersService",
            };
            foreach (string dirPath in dirsToDelete)
                Directory.Delete(dirPath, recursive: true);

            string[] filesToDelete = new[]
            {
                $"{projectSourcePath}/webAPI.Application/Services/Repositories/IEmailAuthenticatorRepository.cs",
                $"{projectSourcePath}/webAPI.Application/Services/Repositories/IOperationClaimRepository.cs",
                $"{projectSourcePath}/webAPI.Application/Services/Repositories/IOtpAuthenticatorRepository.cs",
                $"{projectSourcePath}/webAPI.Application/Services/Repositories/IRefreshTokenRepository.cs",
                $"{projectSourcePath}/webAPI.Application/Services/Repositories/IUserOperationClaimRepository.cs",
                $"{projectSourcePath}/webAPI.Application/Services/Repositories/IUserRepository.cs",
                $"{corePackagePath}/Core.Persistence/Configurations/EmailAuthenticatorConfiguration.cs",
                $"{corePackagePath}/Core.Persistence/Configurations/OperationClaimConfiguration.cs",
                $"{corePackagePath}/Core.Persistence/Configurations/OtpAuthenticatorConfiguration.cs",
                $"{corePackagePath}/Core.Persistence/Configurations/RefreshTokenConfiguration.cs",
                $"{corePackagePath}/Core.Persistence/Configurations/UserConfiguration.cs",
                $"{corePackagePath}/Core.Persistence/Configurations/UserOperationClaimConfiguration.cs",
                $"{projectSourcePath}/webAPI.Persistence/Repositories/EmailAuthenticatorRepository.cs",
                $"{projectSourcePath}/webAPI.Persistence/Repositories/OperationClaimRepository.cs",
                $"{projectSourcePath}/webAPI.Persistence/Repositories/OtpAuthenticatorRepository.cs",
                $"{projectSourcePath}/webAPI.Persistence/Repositories/RefreshTokenRepository.cs",
                $"{projectSourcePath}/webAPI.Persistence/Repositories/UserOperationClaimRepository.cs",
                $"{projectSourcePath}/webAPI.Persistence/Repositories/UserRepository.cs",
                $"{projectSourcePath}/webAPI/Controllers/AuthController.cs",
                $"{projectSourcePath}/webAPI/Controllers/OperationClaimsController.cs",
                $"{projectSourcePath}/webAPI/Controllers/UserOperationClaimsController.cs",
                $"{projectSourcePath}/webAPI/Controllers/UsersController.cs",
            };
            foreach (string filePath in filesToDelete)
                File.Delete(filePath);

            await FileHelper.RemoveLinesAsync(
              filePath: $"{projectSourcePath}/webAPI.Application/ApplicationServiceRegistration.cs",
              predicate: line =>
                  (
                      new[]
                      {
                            "using Application.Services.AuthService;",
                            "services.AddScopedWithManagers(typeof(IAuthService).Assembly);",
                      }
                  ).Any(line.Contains)
          );


            await FileHelper.RemoveContentAsync(
                filePath: $"{projectSourcePath}/webAPI/Program.cs",
                contents: new[]
                {
                    "using Core.Security;",
                    "using Core.Security.Encryption;",
                    "using Core.Security.JWT;",
                    "using Core.WebAPI.Extensions.Swagger;",
                    "using Microsoft.AspNetCore.Authentication.JwtBearer;",
                    "using Microsoft.IdentityModel.Tokens;",
                    "using Microsoft.OpenApi.Models;",
                    "using System.Reflection;",
                    "using Swashbuckle.AspNetCore.SwaggerGen;",
                    "using Core.Utilities.ApiDoc;",
                    "using Core.Utilities.Messages;",
                    "builder.Services.AddSecurityServices();",
                    @"TokenOptions? tokenOptions = builder.Configuration.GetSection(""TokenOptions"").Get<TokenOptions>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidIssuer = tokenOptions.Issuer,
        ValidAudience = tokenOptions.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = SecurityKeyHelper.CreateSecurityKey(tokenOptions.SecurityKey)
    };
});

builder.Services.AddSwaggerGen(opt =>
{
    opt.CustomSchemaIds(type => type.ToString());
    opt.SwaggerDoc(ProjectSwaggerMessages.Version, new OpenApiInfo
    {
        Version = ProjectSwaggerMessages.Version,
        Title = ProjectSwaggerMessages.Title,
        Description = ProjectSwaggerMessages.Description
    });
    opt.CustomOperationIds(apiDesc =>
    {
        return apiDesc.TryGetMethodInfo(out MethodInfo methodInfo)
            ? $""{methodInfo.DeclaringType.Name}.{methodInfo.Name}""
            : new Guid().ToString();
    });
    opt.AddSecurityDefinition(""Bearer"", new OpenApiSecurityScheme
    {
        Name = ""Authorization"",
        Type = SecuritySchemeType.ApiKey,
        Scheme = ""Bearer"",
        BearerFormat = ""JWT"",
        In = ParameterLocation.Header,
        Description =
            ""JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \""Bearer 12345.54321\""""
    });

    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
                { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = ""Bearer"" } },
            new string[] { }
        }
    });
    opt.OperationFilter<AddAuthHeaderOperationFilter>();
    var xmlFile = $""{Assembly.GetExecutingAssembly().GetName().Name}.xml"";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    opt.IncludeXmlComments(xmlPath);
});",
@"app.UseAuthentication();
app.UseAuthorization();"
                }
            );
        }
        private static async Task RemoveProjectsFromSolutionAsync(string solutionFilePath, List<string> projectGuidsToRemove)
        {
            string[] solutionLines = await File.ReadAllLinesAsync(solutionFilePath);
            List<string> newSolutionLines = new List<string>();

            bool shouldRemoveCurrentSection = false;
            foreach (string line in solutionLines)
            {
                if (line.StartsWith("Project("))
                {
                    shouldRemoveCurrentSection = projectGuidsToRemove.Any(guid => line.Contains(guid));
                }

                if (!shouldRemoveCurrentSection)
                {
                    newSolutionLines.Add(line);
                }

                if (line.StartsWith("EndProject"))
                {
                    shouldRemoveCurrentSection = false;
                }
            }

            await File.WriteAllLinesAsync(solutionFilePath, newSolutionLines);
        }
    }
}
