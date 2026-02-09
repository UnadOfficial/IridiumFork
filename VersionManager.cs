using System;

namespace Iridium
{
    public enum VersionType
    {
        Release,
        Beta,
        Prerelease
    }

    public static class VersionManager
    {
        public const VersionType Type = VersionType.Prerelease;
        public const int MinorVersion = 1;

        public static string GetFullVersionString()
        {
            string baseVersion = Main.Mod?.Info.Version ?? "1.0.0";
            if (Type == VersionType.Release)
            {
                return baseVersion;
            }
            return $"{baseVersion}-{Type.ToString().ToLower()}{MinorVersion}";
        }
    }
}
