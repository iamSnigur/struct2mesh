using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using UnityEngine.UIElements;

namespace WebBridges
{
    public static class ClipboardBridge
    {
        private delegate void OverlayClickCallback(float normalizedClickX);

        [DllImport("__Internal")]
        private static extern void InitCopyOverlayInternal(string text, OverlayClickCallback onClick);

        [DllImport("__Internal")]
        private static extern void SetCopyOverlayRectInternal(float x, float y, float w, float h);

        private static VisualElement s_overlayTarget;

        public static void CopyToClipboard(string text)
        {
            // On WebGL the copy is done by the DOM overlay
#if UNITY_EDITOR && UNITY_EDITOR_WIN
            GUIUtility.systemCopyBuffer = text;
#endif
        }

        public static void AttachCopyOverlay(VisualElement target, string text)
        {
            s_overlayTarget = target;
            InitCopyOverlayInternal(text, HandleOverlayClick);
            target.RegisterCallback<GeometryChangedEvent>(_ => UpdateOverlayRect());
            UpdateOverlayRect();
        }

        private static void UpdateOverlayRect()
        {
            IPanel panel = s_overlayTarget?.panel;
            if (panel == null)
                return;

            Rect bounds = s_overlayTarget.worldBound;
            Rect panelBounds = panel.visualTree.worldBound;
            if (panelBounds.width <= 0f || panelBounds.height <= 0f)
                return;

            SetCopyOverlayRectInternal(
                (bounds.x - panelBounds.x) / panelBounds.width,
                (bounds.y - panelBounds.y) / panelBounds.height,
                bounds.width / panelBounds.width,
                bounds.height / panelBounds.height);
        }

        [MonoPInvokeCallback(typeof(OverlayClickCallback))]
        private static void HandleOverlayClick(float normalizedClickX)
        {
            if (s_overlayTarget?.panel == null)
                return;

            Rect bounds = s_overlayTarget.worldBound;
            var systemEvent = new Event
            {
                type = EventType.MouseUp,
                button = 0,
                clickCount = 1,
                mousePosition = new Vector2(
                    bounds.x + normalizedClickX * bounds.width,
                    bounds.center.y)
            };

            using ClickEvent click = ClickEvent.GetPooled(systemEvent);
            click.target = s_overlayTarget;
            s_overlayTarget.SendEvent(click);
        }
    }
}
