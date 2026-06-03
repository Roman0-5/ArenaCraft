using UnityEngine;
using UnityEngine.UI;

namespace ArenaCraft
{
    /// <summary>
    /// Helper for assembling the entire UI in code (GDD 7.3 lists the HUD/menu
    /// elements as "created in Unity UI toolkit"). Uses the built-in legacy font
    /// so no TextMeshPro "Import Essentials" editor step is required.
    /// </summary>
    public static class UIFactory
    {
        private static Font _font;
        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                return _font;
            }
        }

        public static RectTransform Rect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            return rt;
        }

        public static GameObject Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<UnityEngine.UI.Image>().color = color;
            return go;
        }

        public static Image Image(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.color = color;
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int size,
            Color color, TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>A filled bar (background + foreground fill). Returns the fill Image.</summary>
        public static Image Bar(string name, Transform parent, Color bg, Color fill, bool vertical = false)
        {
            var root = Panel(name, parent, bg);
            var fillImg = Image("Fill", root.transform, fill);
            fillImg.type = UnityEngine.UI.Image.Type.Filled;
            fillImg.fillMethod = vertical ? UnityEngine.UI.Image.FillMethod.Vertical
                                          : UnityEngine.UI.Image.FillMethod.Horizontal;
            fillImg.fillOrigin = vertical ? (int)UnityEngine.UI.Image.OriginVertical.Bottom
                                          : (int)UnityEngine.UI.Image.OriginHorizontal.Left;
            fillImg.fillAmount = 1f;
            Stretch(fillImg.rectTransform, 2f);
            return fillImg;
        }

        /// <summary>A clickable button with a centered label. Returns the Button.</summary>
        public static Button Button(string name, Transform parent, string text, int fontSize,
            Color bg, Color textColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.color = bg;
            var btn = go.GetComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = bg;
            colors.highlightedColor = Color.Lerp(bg, Color.white, 0.25f);
            colors.selectedColor = Color.Lerp(bg, Color.white, 0.25f);
            colors.pressedColor = Color.Lerp(bg, Color.black, 0.2f);
            btn.colors = colors;

            var label = Label("Text", go.transform, text, fontSize, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(label.rectTransform);
            return btn;
        }

        /// <summary>Anchor a rect to fill its parent with an optional uniform inset.</summary>
        public static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        public static void Anchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
        }
    }
}
