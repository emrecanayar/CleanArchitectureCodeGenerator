using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Core.CodeGen.CommandLine;

public static class CommandLineHelper
{
    public static string GetOSCommandLine() =>
          RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" :
          RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/bin/bash" :
          RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "/bin/sh" :
          throw new PlatformNotSupportedException("Unsupported operating system.");

    public static async Task RunCommandAsync(string command)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be empty.", nameof(command));

            ProcessStartInfo startInfo = new()
            {
                FileName = GetOSCommandLine(),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = new() { StartInfo = startInfo };
            process.Start();

            await process.StandardInput.WriteLineAsync(command);
            process.StandardInput.Close();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(output))
                Console.WriteLine("Output:\n" + output);

            if (!string.IsNullOrWhiteSpace(error))
                Console.WriteLine("Error:\n" + error);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while executing the command: {ex.Message}");
        }
    }
}
