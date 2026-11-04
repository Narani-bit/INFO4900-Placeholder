using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CercleVR
{
    // Fixed typography in a world-space Canvas, attached to the physical sheet.
    // One pixel is one millimetre. Text never grows to fill unused page space.
    public static class PaperLayout
    {
        public const float PixelsPerMetre = 1000f;
        public const int TitleSize = 48;
        public const int BodySize = 40;
        public const int FooterSize = 30;
        public const int ButtonSize = 36;

        public static Transform CreateCanvas(Transform parent, string name, float width, float height, float depth)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            // Set the render mode BEFORE assigning the rect: an overlay Canvas can drive its size.
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.localPosition = new Vector3(0, 0, depth);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one / PixelsPerMetre;
            rect.sizeDelta = new Vector2(width, height) * PixelsPerMetre;
            rect.pivot = new Vector2(0.5f, 0.5f);
            // No GraphicRaycaster: the existing 3D colliders handle PC/XR selection.
            return rect;
        }

        public static Text Label(Transform canvas, Font font, string name,
            float x, float y, float width, float height, int fontSize, Color color,
            string content = "", TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvas, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, y) * PixelsPerMetre;
            rect.sizeDelta = new Vector2(width, height) * PixelsPerMetre;
            var label = go.GetComponent<Text>();
            label.font = font;
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Normal;
            label.color = color;
            label.alignment = alignment;
            label.lineSpacing = 1.15f;
            label.resizeTextForBestFit = false;
            label.supportRichText = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            label.text = content;
            return label;
        }

        public static void Rule(Transform canvas, float x, float y, float width, Color color)
        {
            var go = new GameObject("Filet imprime", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvas, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0, 0.5f);
            rect.anchoredPosition = new Vector2(x, y) * PixelsPerMetre;
            rect.sizeDelta = new Vector2(width * PixelsPerMetre, 1.5f);
            var line = go.GetComponent<Image>();
            line.color = color;
            line.raycastTarget = false;
        }

        public static List<string> Pages(Text label, string content)
        {
            // Measure with the SAME font, width and line spacing as the rendered text.
            var rect = label.rectTransform.rect;
            var settings = label.GetGenerationSettings(new Vector2(rect.width, 0));
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            var generator = label.cachedTextGeneratorForLayout;
            float pixelsPerUnit = label.pixelsPerUnit;
            return Paginate(content, candidate =>
                generator.GetPreferredHeight(candidate, settings) / pixelsPerUnit <= rect.height - 2f);
        }

        // The measurement is injected so the pagination can also be verified outside Unity.
        // Keep paragraphs (including a suspect's complete journal entry) on one page when possible.
        public static List<string> Paginate(string content, Func<string, bool> fits)
        {
            var pages = new List<string>();
            string page = "";
            string normalized = (content ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
            foreach (string paragraph in normalized.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                string block = paragraph;
                string candidate = page.Length == 0 ? block : page + "\n\n" + block;
                if (fits(candidate)) { page = candidate; continue; }
                if (page.Length > 0) { pages.Add(page); page = ""; }

                while (!fits(block))
                {
                    int low = 0, high = block.Length;
                    while (low < high)
                    {
                        int middle = low + (high - low + 1) / 2;
                        if (fits(block.Substring(0, middle))) low = middle;
                        else high = middle - 1;
                    }
                    if (low == 0)
                        throw new InvalidOperationException("La zone de texte ne peut pas contenir un caractere.");

                    // Prefer a word boundary; split a single oversized word only when necessary.
                    int cut = low;
                    if (cut < block.Length && !char.IsWhiteSpace(block[cut]))
                        for (int i = cut - 1; i > 0; i--)
                            if (char.IsWhiteSpace(block[i])) { cut = i; break; }
                    pages.Add(block.Substring(0, cut).TrimEnd());
                    block = block.Substring(cut).TrimStart();
                }
                page = block;
            }
            if (page.Length > 0 || pages.Count == 0) pages.Add(page);
            return pages;
        }
    }
}
