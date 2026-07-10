using System;

namespace Iridium.Patches
{
    internal sealed class PatchDefinition
    {
        public PatchDefinition(Type type, Func<bool> condition, Type? parent = null)
        {
            Type = type;
            Condition = condition;
            Parent = parent;
            Name = type.Name;
        }

        public Type Type { get; }
        public Func<bool> Condition { get; }
        public Type? Parent { get; }
        public string Name { get; }
    }
}
