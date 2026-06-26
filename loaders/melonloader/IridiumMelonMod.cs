using MelonLoader;
using MelonLoader.TinyJSON;
using System;
using System.IO;
using System.Runtime.CompilerServices;

[assembly: MelonInfo(typeof(Iridium.IridiumMelonMod), BuildInfo.ModName, BuildInfo.ModVersion, BuildInfo.ModAuthor)]
[assembly: MelonGame("7th Beat Games", "A Dance of Fire and Ice")]

namespace Iridium
{
    public class IridiumMelonMod : MelonMod
    {
        private MelonHandler? _handler;
        private bool loaded;

        public override void OnInitializeMelon()
        {
            _handler = new MelonHandler(this);
            Iridium.Main.Initialize(_handler);
        }

        public override void OnUpdate()
        {
            if (!loaded)
                AppDomain.CurrentDomain.Load(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(MelonAssembly.Location), "Iridium.dll")));
            loaded = true;

            UpdateInject();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UpdateInject()
        {
            _handler?.TriggerUpdate(UnityEngine.Time.deltaTime);
        }

        public override void OnGUI()
        {
            _handler?.TriggerGUI();
        }
    }
}
