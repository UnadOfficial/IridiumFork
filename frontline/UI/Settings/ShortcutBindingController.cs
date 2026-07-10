using System;
using Iris.Iml;
using UnityEngine;

namespace Iridium.UI.SettingsPanel
{
    internal sealed class ShortcutBindingController
    {
        private readonly Iridium.Settings _settings;
        private bool _isBinding;
        private string? _bindingTarget;
        private int _bindingOldKey;
        private int _bindingOldMods;

        public ShortcutBindingController(Iridium.Settings settings)
        {
            _settings = settings;
        }

        public void RegisterHandlers(IrisGuiRenderer renderer)
        {
            var keys = new (string Name, Action Handler)[]
            {
                ("selectAll", () => StartBinding("selectAll")),
                ("deselectAll", () => StartBinding("deselectAll")),
                ("toggleVisibility", () => StartBinding("toggleVisibility")),
                ("focusDecoration", () => StartBinding("focusDecoration")),
                ("goToFloor", () => StartBinding("goToFloor")),
                ("selectAllFloors", () => StartBinding("selectAllFloors")),
                ("popupSave", () => StartBinding("popupSave")),
                ("popupDiscard", () => StartBinding("popupDiscard")),
            };

            foreach (var (name, handler) in keys)
            {
                var cap = char.ToUpper(name[0]) + name.Substring(1);
                renderer.RegisterHandler($"OnBind{cap}Key", (obj) => handler());
            }

            renderer.RegisterHandler("OnBindEditorPauseKey", (obj) => StartBinding("editorPause"));
        }

        public void CaptureCurrentEvent()
        {
            if (!_isBinding) return;

            var ev = Event.current;
            if (ev == null || ev.type != EventType.KeyDown) return;

            var keyCode = ev.keyCode;
            bool isModifier = keyCode == KeyCode.LeftControl || keyCode == KeyCode.RightControl ||
                keyCode == KeyCode.LeftShift || keyCode == KeyCode.RightShift ||
                keyCode == KeyCode.LeftAlt || keyCode == KeyCode.RightAlt ||
                keyCode == KeyCode.LeftCommand || keyCode == KeyCode.RightCommand;

            int modifiers = 0;
            if (ev.control) modifiers |= 1;
            if (ev.shift) modifiers |= 2;
            if (ev.alt) modifiers |= 4;
            if (ev.command) modifiers |= 8;

            if (isModifier)
            {
                ApplyBinding(_bindingTarget, 0, modifiers);
                ev.Use();
            }
            else if (keyCode != KeyCode.None && keyCode != KeyCode.Escape)
            {
                ApplyBinding(_bindingTarget, (int)keyCode, modifiers);
                _isBinding = false;
                _bindingTarget = null;
                ev.Use();
            }
            else if (keyCode == KeyCode.Escape)
            {
                ApplyBinding(_bindingTarget, _bindingOldKey, _bindingOldMods);
                _isBinding = false;
                _bindingTarget = null;
                ev.Use();
            }
        }

        public string GetDisplay(int key, int modifiers)
        {
            var modStr = string.Empty;
            if ((modifiers & 1) != 0) modStr += "Ctrl+";
            if ((modifiers & 2) != 0) modStr += "Shift+";
            if ((modifiers & 4) != 0) modStr += "Alt+";
            if ((modifiers & 8) != 0) modStr += "Win+";
            if (key == 0 && modStr != string.Empty) return modStr.TrimEnd('+');
            if (key == 0) return "\u9234?";

            var keyName = key >= 32 && key <= 126
                ? ((char)key).ToString()
                : Enum.IsDefined(typeof(KeyCode), key) ? ((KeyCode)key).ToString() : "?";
            return modStr + keyName;
        }

        private void StartBinding(string target)
        {
            GetBinding(target, out _bindingOldKey, out _bindingOldMods);
            _bindingTarget = target;
            _isBinding = true;
        }

        private void GetBinding(string target, out int key, out int mods)
        {
            key = 0;
            mods = 0;

            switch (target)
            {
                case "selectAll": key = _settings.editorShortcuts.selectAllKey; mods = _settings.editorShortcuts.selectAllModifiers; break;
                case "deselectAll": key = _settings.editorShortcuts.deselectAllKey; mods = _settings.editorShortcuts.deselectAllModifiers; break;
                case "toggleVisibility": key = _settings.editorShortcuts.toggleVisibilityKey; mods = _settings.editorShortcuts.toggleVisibilityModifiers; break;
                case "focusDecoration": key = _settings.editorShortcuts.focusDecorationKey; mods = _settings.editorShortcuts.focusDecorationModifiers; break;
                case "goToFloor": key = _settings.editorShortcuts.goToFloorKey; mods = _settings.editorShortcuts.goToFloorModifiers; break;
                case "selectAllFloors": key = _settings.editorShortcuts.selectAllFloorsKey; mods = _settings.editorShortcuts.selectAllFloorsModifiers; break;
                case "popupSave": key = _settings.editorShortcuts.popupSaveKey; mods = _settings.editorShortcuts.popupSaveModifiers; break;
                case "popupDiscard": key = _settings.editorShortcuts.popupDiscardKey; mods = _settings.editorShortcuts.popupDiscardModifiers; break;
                case "editorPause": key = _settings.compatibility.editorPauseKey; mods = _settings.compatibility.editorPauseModifiers; break;
            }
        }

        private void ApplyBinding(string? target, int key, int mods)
        {
            switch (target)
            {
                case "selectAll": _settings.editorShortcuts.selectAllKey = key; _settings.editorShortcuts.selectAllModifiers = mods; break;
                case "deselectAll": _settings.editorShortcuts.deselectAllKey = key; _settings.editorShortcuts.deselectAllModifiers = mods; break;
                case "toggleVisibility": _settings.editorShortcuts.toggleVisibilityKey = key; _settings.editorShortcuts.toggleVisibilityModifiers = mods; break;
                case "focusDecoration": _settings.editorShortcuts.focusDecorationKey = key; _settings.editorShortcuts.focusDecorationModifiers = mods; break;
                case "goToFloor": _settings.editorShortcuts.goToFloorKey = key; _settings.editorShortcuts.goToFloorModifiers = mods; break;
                case "selectAllFloors": _settings.editorShortcuts.selectAllFloorsKey = key; _settings.editorShortcuts.selectAllFloorsModifiers = mods; break;
                case "popupSave": _settings.editorShortcuts.popupSaveKey = key; _settings.editorShortcuts.popupSaveModifiers = mods; break;
                case "popupDiscard": _settings.editorShortcuts.popupDiscardKey = key; _settings.editorShortcuts.popupDiscardModifiers = mods; break;
                case "editorPause": _settings.compatibility.editorPauseKey = key; _settings.compatibility.editorPauseModifiers = mods; break;
            }

            _settings.Save();
        }
    }
}
