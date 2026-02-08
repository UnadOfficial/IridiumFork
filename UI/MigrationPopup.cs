using UnityEngine;
using Iridium.UI;
using Iridium.Config;

namespace Iridium
{
    public class MigrationPopup : MonoBehaviour
    {
        private bool _show;
        private Rect _windowRect = new Rect(Screen.width / 2 - 200, Screen.height / 2 - 100, 400, 200);

        public static void Create()
        {
            if (!Main.Settings.appearance.needsMigration) return;
            var go = new GameObject("Iridium_MigrationPopup");
            DontDestroyOnLoad(go);
            go.AddComponent<MigrationPopup>();
        }

        private void Start()
        {
            _show = true;
            UIUtils.InitializeStyles();
        }

        private void OnGUI()
        {
            if (!_show) return;

            GUI.skin = null; // Reset to default skin for safety
            _windowRect = GUI.Window(999, _windowRect, DrawWindow, Localization.Get("MigrationTitle"));
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical(UIUtils.CardStyle);
            
            GUILayout.Label(Localization.Get("MigrationDesc"), UIUtils.LabelStyle);
            
            GUILayout.FlexibleSpace();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Localization.Get("Migrate"), UIUtils.ButtonStyle))
            {
                PlaylistManager.MigrateLegacySettings();
                _show = false;
                Destroy(gameObject);
            }
            
            if (GUILayout.Button(Localization.Get("Cancel"), UIUtils.ButtonStyle))
            {
                _show = false;
                Destroy(gameObject);
            }
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}
