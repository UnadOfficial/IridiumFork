namespace Iridium.MargeMods.AsyncInputOptimize
{
    public static class AsyncInputData
    {
        public static ulong currFrameTick;
        public static ulong prevFrameTick;
        public static ulong offsetTick;
        public static ulong offsetTick_REAL;
        public static ulong[] offsetTicks = new ulong[30];
        public static int offsetTicksIndex;
        public static readonly ulong START_TIME = 504911520000000000UL; // 不是 一个只读你凭什么不让我大写啊
        public static double dspTime;
    }
}
