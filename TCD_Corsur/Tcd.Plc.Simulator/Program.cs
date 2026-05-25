using Tcd.Plc.Simulator;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("╔══════════════════════════════════╗");
Console.WriteLine("║   TCD PLC Simulator  v1.0        ║");
Console.WriteLine("╚══════════════════════════════════╝");

int port = 7002;
if (args.Length > 0 && int.TryParse(args[0], out var p)) port = p;

Console.WriteLine($"Port : {port}");
Console.WriteLine("IO Map:");
Console.WriteLine("  DI B0.0=EStop_OK  B0.1=DoorClosed  B1.6=MaterialLow  B1.7=MaterialHigh");
Console.WriteLine("  AI W0=Pressure(kPa*100)  W2=Loadcell(N*10)  W4=Vacuum(kPa*10)");
Console.WriteLine();

using var server = new PlcSimServer(port);

Console.CancelKeyPress += (_, e) =>
{
  e.Cancel = true;
  server.Dispose();
};

await server.RunAsync();
