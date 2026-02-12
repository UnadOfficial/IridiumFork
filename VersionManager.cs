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
        public static VersionType Type => VersionType.Release;
        public const int MinorVersion = 0;

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
