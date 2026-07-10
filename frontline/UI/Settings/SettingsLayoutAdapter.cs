using Iris.Iml;
using UnityEngine;

namespace Iridium.UI.SettingsPanel
{
    internal sealed class SettingsLayoutAdapter : IIrrLayout
    {
        public void BeginHorizontal(IrrContStyle style, GUILayoutOption[] options)
            => IridiumLayout.Begin(IridiumLayout.ContainerDirection.Horizontal, (IridiumLayout.ContainerStyle)(int)style, null, options);

        public void BeginVertical(IrrContStyle style, GUILayoutOption[] options)
            => IridiumLayout.Begin(IridiumLayout.ContainerDirection.Vertical, (IridiumLayout.ContainerStyle)(int)style, null, options);

        public void End() => IridiumLayout.End();

        public bool Button(string text, IrrButStyle style)
            => IridiumLayout.Button(text, (IridiumLayout.ButtonStyle)(int)style);

        public void Text(string text, IrrTextStyle style)
            => IridiumLayout.Text(text, (IridiumLayout.TextStyle)(int)style);

        public bool? Switch(bool on) => IridiumLayout.Switch(on);
        public bool? Checkbox(bool on) => IridiumLayout.Checkbox(on);
        public void Separator() => IridiumLayout.Separator();
        public void Space(double size) => IridiumLayout.Space(size);
        public void Fill() => IridiumLayout.Fill();
        public string? TextField(string content) => IridiumLayout.TextField(content);

        public bool Icon(IrrIconStyle style)
            => IridiumLayout.Icon((IridiumLayout.IconStyle)(int)style);
    }
}
