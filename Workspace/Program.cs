using System;

public static class Program
{
    public static int Main(string[] args)
    {
        try { return WorkspaceCommand.Run(args, Console.Out); }
        catch (Exception error) when (error is ArgumentException or System.IO.IOException or NotSupportedException)
        {
            Console.Error.WriteLine(error.Message);
            return 1;
        }
    }
}
