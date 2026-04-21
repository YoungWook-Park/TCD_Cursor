using Tcd.Robot.Simulator;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("╔══════════════════════════════════╗");
Console.WriteLine("║   TCD Robot Simulator  v1.0      ║");
Console.WriteLine("╚══════════════════════════════════╝");

int port = 7001;
if (args.Length > 0 && int.TryParse(args[0], out var p)) port = p;

Console.WriteLine($"Port : {port}");
Console.WriteLine($"Positions: Home(0) Ready(10) S1_Wait(11) S1_Pick(12) " +
                  $"S2_Wait(13) S2_Pick(14)");
Console.WriteLine($"           UC_Wait(15) UC_Pick(16) LC_Wait(17) " +
                  $"LC_Pick(18) Peel(19)");
Console.WriteLine();

using var server = new SimulatorServer(port);

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    server.Dispose();
};

await server.RunAsync();
