using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.App.Core;
using Tcd.Core;
using Tcd.Sequence;

namespace Tcd.App.Sequences;

/// <summary>
/// 모든 명시적 시퀀스 클래스의 공통 기반 (Template Method 패턴).
/// DeviceActionAsync → PostActionAsync 순서로 실행하며,
/// OperationCanceledException → Stopped, Exception → Alarm + Fail 자동 처리.
/// </summary>
public abstract class SequenceBase : ISequence
{
    /// <summary>MainCore가 필요한 시퀀스용 의존성 필드. 기본값 null! — 파생 클래스가 base(core)로 초기화.</summary>
    protected readonly MainCore _core = null!;

    protected SequenceBase() { }

    protected SequenceBase(MainCore core)
    {
        _core = core ?? throw new ArgumentNullException(nameof(core));
    }

    public abstract string Key { get; }
    public abstract string DisplayName { get; }

    public async Task<SequenceResult> ExecuteAsync(ISequenceContext context, object parameter, CancellationToken cancellationToken)
    {
        try
        {
            await DeviceActionAsync(context, parameter, cancellationToken).ConfigureAwait(false);
            await PostActionAsync(context, cancellationToken).ConfigureAwait(false);
            return SequenceResult.Success();
        }
        catch (OperationCanceledException)
        {
            return SequenceResult.Stopped();
        }
        catch (Exception ex)
        {
            context.Alarms.Raise(new Alarm("SEQ_ERROR", $"{DisplayName}: {ex.Message}", AlarmSeverity.Error, context.Time.Now));
            return SequenceResult.Fail(ex.Message);
        }
    }

    /// <summary>실제 디바이스 명령 실행. 파생 클래스에서 구현.</summary>
    protected abstract Task DeviceActionAsync(ISequenceContext context, object parameter, CancellationToken cancellationToken);

    /// <summary>
    /// 동작 완료 후 상태 검증. 기본 구현은 검증 없이 통과.
    /// 상태 전이 확인이 필요한 시퀀스는 반드시 재정의.
    /// </summary>
    protected virtual Task PostActionAsync(ISequenceContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <summary>공통 인터락. 동작 전 StopToken 취소 여부 확인.</summary>
    protected void CheckInterlock(ISequenceContext context)
    {
        context.StopToken.ThrowIfCancellationRequested();
    }
}
