using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Iris.Iml;
using Iridium.Patches;

namespace Iridium.UI
{
    public class VRAMNotificationUI : MonoBehaviour
    {
        private static VRAMNotificationUI? _instance;

        private IrisGoRenderer _renderer = null!;
        private CanvasGroup _canvasGroup = null!;
        private Coroutine? _fadeCoroutine;

        // Cached references — captured right after each Rebuild so UpdateProgress
        // can change text without rebuilding the whole UI tree.
        private Text? _messageText;

        private string _message = "";
        private float _timer = 0f;
        private bool _isPersistent = false;
        private const float FadeDuration = 0.5f;
        private const float DisplayDuration = 2.5f;

        public static void Show(string message)
        {
            EnsureInstance();
            _instance!._isPersistent = false;
            _instance!._timer = FadeDuration + DisplayDuration + FadeDuration;
            _instance!._SetContent(message, iconType: "success", showStop: false);
        }

        public static void ShowPersistent(string message)
        {
            EnsureInstance();
            _instance!._isPersistent = true;
            _instance!._timer = float.MaxValue;
            _instance!._SetContent(message, iconType: "information", showStop: true);
        }

        public static void UpdateProgress(string message)
        {
            if (_instance == null) return;
            _instance._message = message;
            _instance._isPersistent = true;
            _instance._timer = float.MaxValue;
            // Direct text update on the cached reference — no Rebuild, no allocation.
            if (_instance._messageText != null)
                _instance._messageText.text = message;
        }

        public static void Complete()
        {
            if (_instance == null) return;
            _instance._isPersistent = false;
            _instance._timer = FadeDuration + DisplayDuration + FadeDuration;
            _instance._StartFadeOut();
        }

        public static void RunCoroutine(IEnumerator routine)
        {
            EnsureInstance();
            _instance!.StartCoroutine(routine);
        }

        private static void EnsureInstance()
        {
            if (_instance == null)
            {
                var go = new GameObject("Iridium_VRAMNotification");
                _instance = go.AddComponent<VRAMNotificationUI>();
                DontDestroyOnLoad(go);
                _instance._Initialize();
            }
        }

        private void _Initialize()
        {
            // Build our own Canvas + CanvasGroup so we can:
            //   1. avoid the renderer's full-screen dark overlay (created in EnsureRoot)
            //   2. fade the entire notification via a single CanvasGroup
            //   3. sit below ImlWindow's dialog (sortingOrder 32767 vs 32766)
            // Pre-creating RootObject + a placeholder also makes the renderer's
            // "keep child at index 0" loop behave correctly across multiple rebuilds.
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = true;  // so the Stop button can receive clicks

            var canvasGo = new GameObject("VRAMNotificationCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32766;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            // Placeholder: the renderer's RebuildUI loop destroys all children except
            // index 0. Without this anchor, a previous-frame DialogWrapper would be
            // preserved as index 0 and leak each time _SetContent is called.
            var placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(canvasGo.transform, false);

            _renderer = new IrisGoRenderer
            {
                ParentTransform = transform,
                RootObject = canvasGo,
            };

            string imlPath = Path.Combine(
                Main.Handler?.ModPath ?? "",
                "Resources", "ui", "VRAMNotification.iml");

            if (File.Exists(imlPath))
            {
                _renderer.LoadFile(imlPath);
            }
            else
            {
                Main.Logger?.Log($"[VRAMNotificationUI] IML not found: {imlPath}");
            }

            _renderer.RegisterHandler("OnStop", (obj) =>
            {
                LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch.Cancel();
            });
        }

        private void _SetContent(string message, string iconType, bool showStop)
        {
            _message = message;

            _renderer.SetDataContext(new Dictionary<string, object>
            {
                ["message"] = message,
                ["iconType"] = iconType,
                ["showStop"] = showStop,
                ["stopText"] = Localization.Get("LoadingDecorationsStop"),
            });
            _renderer.Rebuild();

            // Renderer positions the DialogWrapper at screen center by default; we
            // want a small panel pinned to the top-left. Also re-cache refs (the
            // wrapper + children are recreated on every Rebuild).
            _PostProcessLayout();
            _CacheUIRefs();

            _StartFadeIn();
        }

        private void _PostProcessLayout()
        {
            if (_renderer.RootObject == null) return;
            // The renderer always wraps its content in a "DialogWrapper" RectTransform
            // anchored at (0.5, 0.5). For a notification we want top-left instead.
            var wrapper = _renderer.RootObject.transform.Find("DialogWrapper") as RectTransform;
            if (wrapper != null)
            {
                wrapper.anchorMin = new Vector2(0, 1);
                wrapper.anchorMax = new Vector2(0, 1);
                wrapper.pivot = new Vector2(0, 1);
                wrapper.anchoredPosition = new Vector2(20, -20);
            }
            // Make the panel background non-blocking so the rest of the screen still
            // receives clicks. The Stop button itself keeps its raycastTarget.
            var hbox = wrapper?.Find("HBox");
            if (hbox != null)
            {
                var img = hbox.GetComponent<Image>();
                if (img != null) img.raycastTarget = false;
            }
        }

        private void _CacheUIRefs()
        {
            _messageText = null;
            if (_renderer.RootObject == null) return;
            // Find the inner Text component (the one bound to "{message}"). The
            // renderer names the UGUI Text GameObject "Text".
            var all = _renderer.RootObject.GetComponentsInChildren<Text>(true);
            foreach (var t in all)
            {
                if (t.gameObject.name == "Text")
                {
                    _messageText = t;
                    break;
                }
            }
        }

        private void _StartFadeIn()
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(_FadeCanvasGroup(0, 1, FadeDuration));
        }

        private void _StartFadeOut()
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(_FadeCanvasGroup(_canvasGroup.alpha, 0, FadeDuration));
        }

        private IEnumerator _FadeCanvasGroup(float from, float to, float duration)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            _canvasGroup.alpha = to;
            _fadeCoroutine = null;
        }

        private void Update()
        {
            if (_timer > 0f && !_isPersistent) _timer -= Time.deltaTime;
            if (_timer <= 0f && !_isPersistent && _fadeCoroutine == null && _canvasGroup.alpha >= 1f)
            {
                _StartFadeOut();
            }
        }
    }
}
