using Application.Features.Create.Commands.New;
using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ConsoleUI.Commands.New;

public partial class CreateNewProjectCliCommand : AsyncCommand<CreateNewProjectCliCommand.Settings>
{
    private readonly IMediator _mediator;

    public CreateNewProjectCliCommand(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(paramName: nameof(mediator));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        settings.CheckProjectNameArgument();
        settings.CheckIsThereSecurityMechanismArgument();
        settings.CheckIsThereAdminProjectArgument();

        var request = new CreateNewProjectCommand(
            projectName: settings.ProjectName!,
            isThereSecurityMechanism: settings.IsThereSecurityMechanism,
            isThereAdminProject: settings.IsThereAdminProject
        );

        var resultsStream = _mediator.CreateStream(request);

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots2)
            .SpinnerStyle(Style.Parse("blue"))
            .StartAsync("Creating...", async ctx =>
            {
                await foreach (var result in resultsStream)
                {
                    ctx.Status(result.CurrentStatusMessage);

                    if (!string.IsNullOrWhiteSpace(result.LastOperationMessage))
                    {
                        AnsiConsole.MarkupLine($":check_mark_button: {result.LastOperationMessage}");
                    }

                    if (result.NewFilePathsResult is not null && result.NewFilePathsResult.Count > 0)
                    {
                        AnsiConsole.MarkupLine(":new_button: [green]Generated files:[/]");
                        foreach (var filePath in result.NewFilePathsResult)
                        {
                            AnsiConsole.Write(new TextPath(NormalizePath(filePath))
                                .StemColor(Color.Yellow)
                                .LeafColor(Color.Blue));
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(result.OutputMessage))
                    {
                        AnsiConsole.MarkupLine(result.OutputMessage);
                    }
                }
            });

        return 0;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace("\\", Path.DirectorySeparatorChar.ToString())
                   .Replace("/", Path.DirectorySeparatorChar.ToString());
    }
}
