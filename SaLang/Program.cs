using System;
using System.IO;
namespace SaLang;

// Engine
public static partial class Interpreter
{
    public static int Main(string[] args)
    {
        if (args.Length > 0)
            return RunFile(args[0]);
        else
        {
            RunRepl();
            return 0;
        }
    }

    /// <summary>
    /// Interactive mode: reads line by line until EOF or empty line.
    /// </summary>
    private static void RunRepl()
    {
        while (true)
        {
            Console.Write("> ");
            string code = Console.ReadLine();
            if (string.IsNullOrEmpty(code))
                break;

            var result = Execute(code ,"<interactive>");
            if (result.IsError)
                Console.WriteLine(result);
            else
                Console.WriteLine($"[finished] << {result}");
        }
    }

    /// <summary>
    /// Resolves the path, loads the entire file, and executes its contents.
    /// Returns 0 on success, 1 on error (file not found or runtime error).
    /// </summary>
    private static int RunFile(string userPath)
    {
        string filePath;
        try
        {
            filePath = ResolveFilePath(userPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error resolving path: " + ex.Message);
            return 1;
        }

        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"File not found: {filePath}");
            return 1;
        }

        string source;
        try
        {
            source = File.ReadAllText(filePath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error reading file: " + ex.Message);
            return 1;
        }

        var result = Execute(source, filePath);
        if (result.IsError)
        {
            Console.Error.WriteLine(result);
            return 1;
        }

        Console.WriteLine($"output result: {result}");
        return 0;
    }

    /// <summary>
    /// Resolves userPath (absolute, relative to CWD or exe directory, '~' expansion).
    /// </summary>
    private static string ResolveFilePath(string userPath)
    {
        if (userPath.StartsWith('~'))
        {
            string home = Environment.GetEnvironmentVariable("HOME")
                    ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            userPath = string.Concat(home, userPath.AsSpan(1));
        }

        if (Path.IsPathRooted(userPath))
            return Path.GetFullPath(userPath);

        // Try relative to the current working directory
        string cwd = Environment.CurrentDirectory;
        string candidate = Path.GetFullPath(Path.Combine(cwd, userPath));
        if (File.Exists(candidate))
            return candidate;

        // Try relative to the executable directory
        string exeDir = AppContext.BaseDirectory;
        candidate = Path.GetFullPath(Path.Combine(exeDir, userPath));
        if (File.Exists(candidate))
            return candidate;

        return Path.GetFullPath(Path.Combine(cwd, userPath));
    }
}
