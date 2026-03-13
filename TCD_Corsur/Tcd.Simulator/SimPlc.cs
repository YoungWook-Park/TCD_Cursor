using System;
using System.Threading;
using System.Threading.Tasks;
using Tcd.Core;
using Tcd.Devices;
using Tcd.Materials;

namespace Tcd.Simulator
{
    public sealed class SimPlc : IPlc
    {
        private readonly ITimeProvider _time;
        private readonly IMaterialTracker _materials;

        public SimPlc(ITimeProvider time, IMaterialTracker materials)
        {
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _materials = materials ?? throw new ArgumentNullException(nameof(materials));
        }

        public async Task<bool> WaitForStageLoadedAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var start = _time.Now;
            while (_time.Now - start < timeout)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var s1 = _materials.Get(MaterialLocation.Stage1);
                var s2 = _materials.Get(MaterialLocation.Stage2);

                if (s1 != null && s2 != null) return true;

                await _time.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
            }
            return false;
        }
    }
}

