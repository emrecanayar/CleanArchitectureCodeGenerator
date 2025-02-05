using Spectre.Console;
using Spectre.Console.Cli;

namespace ConsoleUI.Commands.New;

public partial class CreateNewProjectCliCommand
{
    public class Settings : CommandSettings
    {
        [CommandArgument(position: 0, template: "[ProjectName]")]
        public string? ProjectName { get; set; }

        [CommandOption("--no-security")]
        public bool IsThereSecurityMechanism { get; set; }

        [CommandOption("--no-admin")]
        public bool IsThereAdminProject { get; set; }


        public void CheckProjectNameArgument()
        {
            if (!string.IsNullOrWhiteSpace(ProjectName))
                return;

            ProjectName = AnsiConsole.Ask<string>("What's the project name?").Trim();

            if (string.IsNullOrWhiteSpace(ProjectName))
                throw new ArgumentException("Project name cannot be empty or whitespace.", nameof(ProjectName));
        }

        public void CheckIsThereSecurityMechanismArgument()
        {
            if (IsThereSecurityMechanism)
                return;

            IsThereSecurityMechanism = AnsiConsole.Confirm(
                "Do you want to add a security mechanism to your project?",
                defaultValue: true
            );
        }

        public void CheckIsThereAdminProjectArgument()
        {
            if (IsThereAdminProject)
                return;

            IsThereAdminProject = AnsiConsole.Confirm(
                "Do you want to add an admin project to your project?",
                defaultValue: true
            );
        }
    }
}
