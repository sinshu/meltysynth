// SPDX-License-Identifier: MIT
// Copyright and inherited upstream notices: see LICENSE.txt.

using System;

class Program
{
    [STAThread]
    static void Main()
    {
        using (var game = new Game1())
        {
            game.Run();
        }
    }
}
