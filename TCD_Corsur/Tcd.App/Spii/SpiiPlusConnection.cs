using System;
using System.Linq;
using ACS.SPiiPlusNET;

namespace Tcd.App.Spii;

/// <summary>
/// 얇은 SPII 연결 래퍼: 변수 읽기/쓰기, 버퍼 실행만 담당.
/// ascpl DBUFFER에서 정의한 전역변수/버퍼 이름과 맞춰서 사용합니다.
/// </summary>
public sealed class SpiiPlusConnection : IDisposable
{
    private readonly Api _api = new();

    public SpiiPlusConnection(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentNullException(nameof(ipAddress));

        // Ethernet TCP 연결 (표준 포트)
        _api.OpenCommSimulator();
    }

    public void Dispose()
    {
        try { _api.CloseComm(); } catch { /* ignore */ }
    }

    // ----- 배열 변수 한 인덱스에 double 쓰기 (예: PC_ACS_DISTANCE(axis)) -----
    public void WriteRealAt(string varName, int index, double value)
    {
        _api.WriteVariable(
            new[] { value },
            varName,
            ProgramBuffer.ACSC_NONE,
            index, index,            // row: index~index
            Api.ACSC_NONE, Api.ACSC_NONE // col: all
        );
    }

    // ----- 배열 변수 한 인덱스에 int 쓰기 (예: CMD_ABS_MOVE(axis)) -----
    public void WriteIntAt(string varName, int index, int value)
    {
        _api.WriteVariable(
            new[] { value },
            varName,
            ProgramBuffer.ACSC_NONE,
            index, index,
            Api.ACSC_NONE, Api.ACSC_NONE
        );
    }

    // ----- 단일 int 변수 읽기 (예: ACS_PC_IS_MOVE_AXIS0) -----
    public int ReadInt(string varName)
    {
        var obj = _api.ReadVariableAsMatrix(
            varName,
            ProgramBuffer.ACSC_NONE,
            Api.ACSC_NONE, Api.ACSC_NONE,
            Api.ACSC_NONE, Api.ACSC_NONE
        );

        if (obj is Array arr)
        {
            var values = arr.Cast<object>().Select(Convert.ToInt32).ToArray();
            return values.Length > 0 ? values[0] : 0;
        }
        return Convert.ToInt32(obj);
    }

    // ----- 단일 double 변수 읽기 (예: ACS_PC_CURRENT_POS_AXIS0) -----
    public double ReadReal(string varName)
    {
        var obj = _api.ReadVariableAsMatrix(
            varName,
            ProgramBuffer.ACSC_NONE,
            Api.ACSC_NONE, Api.ACSC_NONE,
            Api.ACSC_NONE, Api.ACSC_NONE
        );

        if (obj is Array arr)
        {
            var values = arr.Cast<object>().Select(Convert.ToDouble).ToArray();
            return values.Length > 0 ? values[0] : 0.0;
        }
        return Convert.ToDouble(obj);
    }

    // ----- 버퍼 실행 (번호만) -----
    public void RunBuffer(int bufferNumber)
    {
        ProgramStates sts = _api.GetProgramState((ProgramBuffer)bufferNumber) & ProgramStates.ACSC_PST_RUN;

        if (sts != ProgramStates.ACSC_PST_RUN)
        {
                _api.RunBuffer(
             (ProgramBuffer)bufferNumber,
             null
         );
        }
     
    }
}

