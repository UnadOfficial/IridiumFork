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
        public static VersionType Type => VersionType.Prerelease;
        public const int MinorVersion = 1;

        public static string GetFullVersionString()
        {
            string baseVersion = Main.Handler?.ModVersion ?? "Error";
            string adofaiSuffix = $"+adofai{BuildInfo.AdofaiVersion}";
            if (Type == VersionType.Release)
            {
                return $"{baseVersion}{adofaiSuffix}";
            }
            return $"{baseVersion}_{Type.ToString().ToLower()}{MinorVersion}{adofaiSuffix}";
        }
    }
}
