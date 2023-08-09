using Core.CrossCuttingConcerns.Exceptions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ConsoleUI.Commands.Generate.Crud;

public partial class GenerateCrudCliCommand
{
    public class Settings : CommandSettings
    {
        [CommandArgument(position: 0, template: "[EntityName]")]
        public string? EntityName { get; set; }

        [CommandArgument(position: 1, template: "[DBContextName]")]
        public string? DbContextName { get; set; }

        [CommandArgument(position: 2, template: "[ProjectName]")]
        public string? ProjectName { get; set; }

        [CommandOption("-s|--secured")]
        public bool IsSecuredOperationUsed { get; set; }

        public string ProjectPath =>
            ProjectName != null
                ? $@"{Environment.CurrentDirectory}\src\corePackages\Core.Domain\Entities"
                : Environment.CurrentDirectory;

        public void CheckProjectName()
        {
            if (ProjectName != null)
            {
                if (!Directory.Exists(ProjectPath))
                    throw new BusinessException($"Project not found in \"{ProjectPath}\".");
                AnsiConsole.MarkupLine($"Selected [green]project[/] is [blue]{ProjectName}[/].");
                return;
            }

            string[] layerFolders = { "webAPI.Application", "webAPI.Domain", "webAPI.Persistence", "webAPI" };
            if (
                layerFolders.All(
                    folder => Directory.Exists($"{Environment.CurrentDirectory}/{folder}")
                )
            )
                return;

            string[] projects = Directory
                .GetDirectories($"{Environment.CurrentDirectory}/src")
                .Select(Path.GetFileName)
                .Where(project => project != "corePackages")
                .ToArray()!;
            if (projects.Length == 0)
                throw new BusinessException("No projects found in src");
            if (projects.Length == 1)
            {
                ProjectName = projects.First();
                AnsiConsole.MarkupLine($"Selected [green]project[/] is [blue]{ProjectName}[/].");
                return;
            }

            ProjectName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What's your [green]project[/] in [blue]src[/] folder?")
                    .PageSize(10)
                    .AddChoices(projects)
            );
        }

        public void CheckEntityArgument()
        {
            if (EntityName is not null)
            {
                AnsiConsole.MarkupLine($"Selected [green]entity[/] is [blue]{EntityName}[/].");
                return;
            }

            string[] entities = Directory
                .GetFiles(path: @$"{ProjectPath}")
                .Select(Path.GetFileNameWithoutExtension)
                .ToArray()!;
            if (entities.Length == 0)
                throw new BusinessException(
                    $"No entities found in \"{ProjectPath}"
                );

            EntityName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What's your [green]entity[/]?")
                    .PageSize(10)
                    .AddChoices(entities)
            );
        }

        public void CheckDbContextArgument()
        {
            if (DbContextName is not null)
            {
                AnsiConsole.MarkupLine(
                    $"Selected [green]DbContext[/] is [blue]{DbContextName}[/]."
                );
                return;
            }

            string persistencePath = $@"{Environment.CurrentDirectory}\src\corePackages\Core.Persistence\Contexts";

            string[] dbContexts = Directory
                .GetFiles(path: persistencePath)
                .Select(Path.GetFileNameWithoutExtension)
                .ToArray()!;
            if (dbContexts.Length == 0)
                throw new BusinessException(
                    $"No DbContexts found in {persistencePath}"
                );

            DbContextName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What's your [green]DbContext[/]?")
                    .PageSize(5)
                    .AddChoices(dbContexts)
            );
        }

        public void CheckMechanismOptions()
        {
            List<string> mechanismsToPrompt = new();

            if (IsSecuredOperationUsed)
                AnsiConsole.MarkupLine("[green]SecuredOperation[/] is used.");
            else
                mechanismsToPrompt.Add("Secured Operation");

            if (mechanismsToPrompt.Count == 0)
                return;

            List<string> selectedMechanisms = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("What [green]mechanisms[/] do you want to use?")
                    .NotRequired()
                    .PageSize(5)
                    .MoreChoicesText("[grey](Move up and down to reveal more mechanisms)[/]")
                    .InstructionsText(
                        "[grey](Press [blue]<space>[/] to toggle a mechanism, "
                            + "[green]<enter>[/] to accept)[/]"
                    )
                    .AddChoices(mechanismsToPrompt)
            );

            selectedMechanisms
                .ToList()
                .ForEach(mechanism =>
                {
                    switch (mechanism)
                    {
                        case "Secured Operation":
                            IsSecuredOperationUsed = true;
                            break;
                    }
                });
        }

        public void CheckProjectsArgument()
        {
            string projectPaths = $@"{Environment.CurrentDirectory}\src\projects";

            string[] projectNames = Directory.GetDirectories(projectPaths)
                .Select(Path.GetFileName)
                .ToArray();

            if (projectNames.Length == 0)
            {

                throw new BusinessException($"No Projects found in {projectPaths}");
            }


            ProjectName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Which [green]Project[/] do you want to select?")
                    .PageSize(5)
                    .AddChoices(projectNames)
            );
        }

    }
}
