// SPDX-License-Identifier: MIT
// Copyright and inherited upstream notices: see LICENSE.txt.

using System;

public static class Program
{
    public static int Main(string[] args)
    {
        try { return WorkspaceCommand.Run(args, Console.Out); }
        catch (Exception error) when (error is ArgumentException or System.IO.IOException or NotSupportedException or System.Text.Json.JsonException)
        {
            Console.Error.WriteLine(error.Message);
            return 1;
        }
    }
}
