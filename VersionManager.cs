using System;

namespace Iridium
{
    public enum VersionType
    {
        Hotfix,
        Release,
        Beta,
        Prerelease
    }

    public static class VersionManager
    {
        public static VersionType Type => VersionType.Beta;
        public const int MinorVersion = 11;

        public static string GetFullVersionString()
        {
            string baseVersion = Main.Mod?.Info.Version ?? "Error";
            if (Type == VersionType.Release)
            {
                return baseVersion;
            }
            return $"{baseVersion}-{Type.ToString().ToLower()}{MinorVersion}";
        }
    }
}
