using System;
using UnityEngine;
using UnityEngine.UI;

namespace Cove
{
    public static class UITheme
    {
        public static Font Font;
        private static Sprite rounded;

        public static void Init()
        {
            if (Font != null) return;
            try { Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { Font = null; }
            if (Font == null)
            {
                try { Font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { Font = null; }
            }
            if (Font == null) Font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            rounded = MakeRoundedSprite(64, 18);
        }

        public static Sprite Rounded
        {
            get
            {
                if (rounded == null) Init();
                return rounded;
            }
        }

        private static Sprite MakeRoundedSprite(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "rounded";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = 0f;
                    float dy = 0f;
                    if (x < radius) dx = radius - x;
                    else if (x >= size - radius) dx = x - (size - radius - 1);
                    if (y < radius) dy = radius - y;
                    else if (y >= size - radius) dy = y - (size - radius - 1);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(radius - dist + 1f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            var border = new Vector4(radius + 1, radius + 1, radius + 1, radius + 1);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        public static GameObject Panel(Transform parent, string name, Color color, bool shadow = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = Rounded;
            img.type = Image.Type.Sliced;
            img.color = color;
            if (shadow)
            {
                var s = go.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.18f);
                s.effectDistance = new Vector2(0f, -4f);
            }
            return go;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, bool shadow = false)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (shadow)
            {
                var s = go.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.45f);
                s.effectDistance = new Vector2(1.5f, -1.5f);
            }
            return t;
        }

        public static Button Button(Transform parent, string label, Action onClick, Color bg, Color fg, int fontSize = 20)
        {
            var go = Panel(parent, "Button", bg, true);
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            colors.colorMultiplier = 1f;
            btn.colors = colors;
            var t = Label(go.transform, label, fontSize, fg, TextAnchor.MiddleCenter, false);
            Stretch(t.rectTransform, 14f, 4f, 14f, 4f);
            if (onClick != null) btn.onClick.AddListener(() =>
            {
                if (GameBoot.I != null && GameBoot.I.Audio != null) GameBoot.I.Audio.Play(GameBoot.I.Audio.Click, 0.5f, 1f);
                onClick();
            });
            return btn;
        }

        public static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        public static void Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static Image Bar(Transform parent, Color bg, Color fill, out RectTransform fillRt, Vector2 size)
        {
            var back = Panel(parent, "Bar", bg);
            back.GetComponent<Image>().raycastTarget = false;
            var backRt = back.GetComponent<RectTransform>();
            backRt.sizeDelta = size;
            var fillGo = Panel(back.transform, "Fill", fill);
            fillGo.GetComponent<Image>().raycastTarget = false;
            fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.offsetMin = new Vector2(3f, 3f);
            fillRt.offsetMax = new Vector2(-3f, -3f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            return back.GetComponent<Image>();
        }
    }
}
