﻿using Core.CodeGen.Code;
using Core.CrossCuttingConcerns.Exceptions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ConsoleUI.Commands.Generate.Query;

public partial class GenerateQueryCliCommand
{
    public class Settings : CommandSettings
    {
        [CommandArgument(position: 0, template: "[QueryName]")]
        public string? QueryName { get; set; }

        [CommandArgument(position: 0, template: "[FeatureName]")]
        public string? FeatureName { get; set; }

        [CommandArgument(position: 1, template: "[ProjectName]")]
        public string? ProjectName { get; set; }

        [CommandOption("-s|--secured")]
        public bool IsSecuredOperationUsed { get; set; }

        public string ProjectPath =>
            ProjectName != null
                ? $@"{Environment.CurrentDirectory}\src\projects\{ProjectName.ToCamelCase()}"
                : Environment.CurrentDirectory;

        public void CheckQueryName()
        {
            if (QueryName is not null)
            {
                AnsiConsole.MarkupLine($"[green]Query[/] name is [blue]{QueryName}[/].");
                return;
            }

            QueryName = AnsiConsole.Prompt(
                new TextPrompt<string>("[blue]What is [green]new query name[/]?[/]")
            );
        }

        public void CheckFeatureName()
        {
            if (FeatureName is not null)
            {
                AnsiConsole.MarkupLine(
                    $"[green]Feature[/] name that the query will be in is [blue]{FeatureName}[/]."
                );
                return;
            }

            string?[] features = Directory
                .GetDirectories($"{ProjectPath}/webAPI.Application/Features")
                .Select(Path.GetFileName)
                .ToArray()!;
            if (features.Length == 0)
                throw new BusinessException(
                    $"No feature found in \"{ProjectPath}/Application/Features\"."
                );

            FeatureName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[blue]Which is [green]feature name[/] that the command will be in?[/]")
                    .PageSize(10)
                    .AddChoices(features)
            );
        }

        public void CheckProjectName()
        {
            if (ProjectName != null)
            {
                if (!Directory.Exists(ProjectPath))
                    throw new BusinessException("Project not found");
                AnsiConsole.MarkupLine($"Selected [green]project[/] is [blue]{ProjectName}[/].");
                return;
            }

            string[] layerFolders = { "Application", "Domain", "Persistence", "WebAPI" };
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
