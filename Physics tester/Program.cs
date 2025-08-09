using Physics_tester;
using System;

public static class Program
{
    [STAThread]
    static void Main()
    {
        using var game = new PhysicsTester();
        game.Run();
    }
}
