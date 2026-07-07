namespace Iridium.MargeMods.AsyncInputOptimize
{
    public static class Main
    {
        private static bool _state;
        public static bool State
        {
            get => _state;
            set
            {
                if (_state ^ value) // 耍杂技.jpg
                {
                    _state = value;
                    if (value) OnModEnabled();
                    else OnModDisabled();
                }
            }
        }
        private static void OnModEnabled()
        {
            SafeDSPTime.Init();
        }
        private static void OnModDisabled()
        {
            SafeDSPTime.Destroy();
        }
    }
}
