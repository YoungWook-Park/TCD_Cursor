using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tcd.Plc.Simulator.Protocol;

internal static class MsgType
{
  public const string ReadAll   = "ReadAll";
  public const string ReadBit   = "ReadBit";
  public const string ReadWord  = "ReadWord";
  public const string WriteBit  = "WriteBit";
  public const string WriteWord = "WriteWord";
  public const string Snapshot  = "Snapshot";
  public const string Ack       = "Ack";
}

internal sealed class PlcRequest
{
  [JsonPropertyName("T")]    public string T    { get; set; } = "";
  [JsonPropertyName("Addr")] public int    Addr { get; set; }
  [JsonPropertyName("Val")]  public int    Val  { get; set; }
}

internal sealed class PlcResponse
{
  [JsonPropertyName("T")]     public string   T     { get; set; } = "";
  [JsonPropertyName("Ok")]    public bool     Ok    { get; set; }
  [JsonPropertyName("Val")]   public int?     Val   { get; set; }
  [JsonPropertyName("Bits")]  public byte[]?  Bits  { get; set; }
  [JsonPropertyName("Words")] public short[]? Words { get; set; }
}

internal static class JsonOpts
{
  public static readonly JsonSerializerOptions Default = new()
  {
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
  };
}
