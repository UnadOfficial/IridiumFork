using System.Runtime.CompilerServices;

namespace Iridium.MargeMods.AsyncInputOptimize
{
    public static class CppBrige
    {
#if WIN32
        [DllImport("Kernel32.dll"), SuppressUnmanagedCodeSecurity]
        private static extern void GetSystemTimePreciseAsFileTime(out ulong lpTime);
#endif //WIN32
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetSystemTick()
        {
#if WIN32
            GetSystemTimePreciseAsFileTime(out var res);
            return res;
#endif //WIN32
        }
    }
}
