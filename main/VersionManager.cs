using System;

namespace Iridium
{
    public enum VersionType
    {
        Hotfix,
        Release,
        Beta,
        Nightly,
        Prerelease
    }

    public static class VersionManager
    {
        public static VersionType Type => VersionType.Release;
        public const int MinorVersion = 0;

        public static string GetFullVersionString()
        {
            string baseVersion = Main.Handler?.ModVersion ?? "Error";
            string adofaiSuffix = $"+adofai_{BuildInfo.AdofaiVersion}";
            if (Type == VersionType.Release)
            {
                return $"{baseVersion}_{adofaiSuffix}";
            }
            return $"{baseVersion}_{Type.ToString().ToLower()}{MinorVersion}{adofaiSuffix}";
        }
    }
}
