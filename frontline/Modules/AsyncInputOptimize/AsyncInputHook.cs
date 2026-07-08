using System.Diagnostics;
using UnityEngine;

using static Iridium.Modules.AsyncInputOptimize.ADORef_scrConductor;

namespace Iridium.Modules.AsyncInputOptimize
{
    public static class AsyncInputHook
    {
        public static void ResetTime()
        {
            // SafeDSPTime.SetOffset(0);
            AsyncInputData.prevFrameTick = AsyncInputData.currFrameTick;
            AsyncInputData.currFrameTick = CppBrige.GetSystemTick();
            AsyncInputData.offsetTick = AsyncInputData.currFrameTick - SafeDSPTime.InterpolationDSPTimeAsFileTime;
            AsyncInputData.dspTime = SafeDSPTime.InterpolationDSPTime;
        }
        public static void PauseTime()
        {
            AsyncInputData.prevFrameTick = AsyncInputData.currFrameTick;
            AsyncInputData.currFrameTick = CppBrige.GetSystemTick();
            AsyncInputData.offsetTick = AsyncInputData.currFrameTick - SafeDSPTime.InterpolationDSPTimeAsFileTime;
            AsyncInputData.dspTime = SafeDSPTime.InterpolationDSPTime;
        }
        public static void ConductorUpdate(scrConductor @this)
        {
        JMP_RELOAD:
            double dspTime = SafeDSPTime.InterpolationDSPTime;
            double time = Time.unscaledTimeAsDouble;
            @this.dspTime = dspTime;
            lastReportedPlayheadPosition.SetValue(@this, dspTime);
            previousFrameTime.SetValue(@this, time);
            if (AsyncInputManager.isActive)
            {
                if (scrController.instance?.paused ?? true)
                {
                    PauseTime();
                    return;
                }
                double audio_precise = SafeDSPTime.GetAuidoPrecise();
                long freq = Stopwatch.Frequency;
                AsyncInputData.prevFrameTick = AsyncInputData.currFrameTick;
                AsyncInputData.currFrameTick = CppBrige.GetSystemTick();
                AsyncInputData.dspTime = (AsyncInputData.currFrameTick - AsyncInputData.offsetTick) / (double)freq;
                AsyncInputData.offsetTick_REAL = AsyncInputData.currFrameTick - SafeDSPTime.InterpolationDSPTimeAsFileTime;
                AsyncInputData.offsetTicks[AsyncInputData.offsetTicksIndex++] = AsyncInputData.offsetTick_REAL;
                long delta = AsyncInputData.offsetTick_REAL - AsyncInputData.offsetTick;

                if (System.Math.Abs(delta) > (long)(audio_precise * freq * 4))
                {
                    AsyncInputData.offsetTicksIndex = 0;
                    AsyncInputData.offsetTick += delta;
                    Iridium.Main.Logger?.Warning("[AsyncInputOptimize] DSPTime XRUN Error");
                    goto JMP_RELOAD;
                }
                if (AsyncInputData.offsetTicksIndex == 30)
                {
                    AsyncInputData.offsetTicksIndex = 0;
                    long datas = 0;
                    foreach (long val in AsyncInputData.offsetTicks)
                        datas += val;
                    datas /= 30;
                    delta = datas - AsyncInputData.offsetTick;
                    if (System.Math.Abs(delta) > (long)(audio_precise * freq / 2))
                    {
                        AsyncInputData.offsetTick += delta;
                        Iridium.Main.Logger?.Log("[AsyncInputOptimize] Offset fix");
                    }
                }

                AsyncInputManager.prevFrameTick = (ulong)AsyncInputData.prevFrameTick;
                AsyncInputManager.currFrameTick = (ulong)AsyncInputData.currFrameTick;
                AsyncInputManager.offsetTick = (ulong)AsyncInputData.offsetTick;
                AsyncInputManager.previousFrameTime = Time.timeAsDouble;
                AsyncInputManager.offsetTickUpdated = true;

                if (ADOBase.controller != null && !ADOBase.controller.paused)
                    ADOBase.controller.UpdateInput();
            }
            @this.prev_dspTime = @this.dspTime;
            @this.prev_unityDspTime = dspTime;
        }
    }
}
