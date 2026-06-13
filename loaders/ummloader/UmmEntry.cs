namespace Iridium.Loader
{
    public static class UmmEntry
    {
        public static bool Load(object modEntry)
        {
            return Main.Initialize(new UmmHandler(modEntry));
        }
    }
}
