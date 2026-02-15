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
        public static VersionType Type => VersionType.Hotfix;
        public const int MinorVersion = 1;

        public static string GetFullVersionString()
        {
            if (Type == VersionType.Release)
            {
                return "1.0.5-hotfix1";
            }
            return $"1.0.5-hotfix1-{Type.ToString().ToLower()}{MinorVersion}";
        }
    }
}
