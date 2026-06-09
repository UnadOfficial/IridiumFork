using MelonLoader;

[assembly: MelonInfo(typeof(Iridium.IridiumMelonMod), "Iridium", "1.3.0", "Xbodwf")]
[assembly: MelonGame("7th Beat Games", "A Dance of Fire and Ice")]

namespace Iridium
{
    public class IridiumMelonMod : MelonMod
    {
        private MelonHandler? _handler;

        public override void OnInitializeMelon()
        {
            _handler = new MelonHandler(this);
            Iridium.Main.Initialize(_handler);
        }

        public override void OnUpdate()
        {
            _handler?.TriggerUpdate(UnityEngine.Time.deltaTime);
        }

        public override void OnGUI()
        {
            _handler?.TriggerGUI();
        }
    }
}
