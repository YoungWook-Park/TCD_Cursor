using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tcd.Robot.Simulator.Protocol;

// ── 메시지 타입 상수 ────────────────────────────────────────────────────────
public static class MsgType
{
    // HMI → Server
    public const string Move        = "Move";
    public const string Stop        = "Stop";
    public const string SetVelocity = "SetVelocity";
    public const string SetTeach    = "SetTeach";
    public const string GetState    = "GetState";

    // Server → HMI
    public const string Ack         = "Ack";
    public const string State       = "State";
    public const string Arrived     = "Arrived";
}

// ── HMI → Server (요청) ────────────────────────────────────────────────────
/// <summary>HMI가 서버로 보내는 커맨드 프레임. 사용하지 않는 필드는 기본값으로 둔다.</summary>
public sealed class RobotRequest
{
    [JsonPropertyName("T")]     public string T     { get; set; } = "";
    /// <summary>포지션 ID (RobotPosition enum 정수값)</summary>
    [JsonPropertyName("Pos")]   public int    Pos   { get; set; }
    /// <summary>속도 퍼센트 0~100 (SetVelocity 전용)</summary>
    [JsonPropertyName("Pct")]   public int    Pct   { get; set; }
    /// <summary>티칭 X 좌표 mm (SetTeach 전용)</summary>
    [JsonPropertyName("X")]     public double X     { get; set; }
    /// <summary>티칭 Y 좌표 mm (SetTeach 전용)</summary>
    [JsonPropertyName("Y")]     public double Y     { get; set; }
    /// <summary>티칭 각도 deg (SetTeach 전용)</summary>
    [JsonPropertyName("Theta")] public double Theta { get; set; }
}

// ── Server → HMI (응답 / Push) ─────────────────────────────────────────────
/// <summary>서버가 HMI로 보내는 프레임. T 값으로 종류를 구분한다.</summary>
public sealed class RobotResponse
{
    [JsonPropertyName("T")]   public string T  { get; set; } = "";

    // Ack 전용
    [JsonPropertyName("Cmd")] public string? Cmd { get; set; }
    [JsonPropertyName("Ok")]  public bool    Ok  { get; set; }
    [JsonPropertyName("Err")] public string? Err { get; set; }

    // State / Arrived 공용
    [JsonPropertyName("Connected")] public bool    Connected { get; set; }
    [JsonPropertyName("Running")]   public bool    Running   { get; set; }
    [JsonPropertyName("Home")]      public bool    Home      { get; set; }
    [JsonPropertyName("Error")]     public bool    Error     { get; set; }
    [JsonPropertyName("Pos")]       public int     Pos       { get; set; }
    [JsonPropertyName("ErrMsg")]    public string? ErrMsg    { get; set; }
}

// ── 직렬화 옵션 공유 ────────────────────────────────────────────────────────
public static class JsonOpts
{
    public static readonly JsonSerializerOptions Default = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
