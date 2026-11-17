using System;
using TMPro;
using UI.Builder;
using UnityEngine;
using UnityEngine.UI;

namespace EnusDE.MultiplayerBrowser
{
    internal sealed class BrowserControls : IDisposable
    {
        private readonly TMP_Text _status;
        private readonly Vector4 _oldMargin;
        private RectTransform _panel;
        private TMP_Text _sort;
        private TMP_Text _restriction;
        private float _width = -1f;

        internal BrowserControls(TMP_Text status, Action sort, Action restriction)
        {
            _status = status;
            _oldMargin = status.margin;
            try
            {
                _panel = MakeRect("EnusDE Browser Controls", status.rectTransform);
                _panel.anchorMin = new Vector2(0.46f, 0);
                _panel.anchorMax = Vector2.one;
                _panel.offsetMin = Vector2.zero;
                _panel.offsetMax = Vector2.zero;
                _sort = MakeButton("Sort", 0f, 0.41f, sort);
                _restriction = MakeButton("New employees", 0.41f, 1f, restriction);
                ((RectTransform)_restriction.transform.parent).Tooltip(
                    "New employees", "Filter using the game's new-employee restrictions.");
                UpdateLabels(SortOrder.Name, false);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal void UpdateLayout()
        {
            float width = _status.rectTransform.rect.width;
            if (Mathf.Approximately(width, _width))
                return;
            _width = width;
            var margin = _oldMargin;
            margin.z += Mathf.Max(0f, width * 0.54f + 4f);
            _status.margin = margin;
        }

        internal void UpdateLabels(SortOrder sort, bool openOnly)
        {
            _sort.text = sort == SortOrder.Name ? "Sort: Name" : "Sort: Players";
            _restriction.text = openOnly ? "Open to new only" : "All railroads";
        }

        public void Dispose()
        {
            if (_status != null)
                _status.margin = _oldMargin;
            if (_panel != null)
            {
                _panel.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(_panel.gameObject);
            }
        }

        private TMP_Text MakeButton(string name, float left, float right, Action action)
        {
            RectTransform rect = MakeRect(name, _panel);
            rect.anchorMin = new Vector2(left, 0f);
            rect.anchorMax = new Vector2(right, 1f);
            rect.offsetMin = new Vector2(3f, 1f);
            rect.offsetMax = new Vector2(-1f, -1f);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.20f, 0.22f, 0.24f, 1f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => action());

            RectTransform labelRect = MakeRect("Label", rect);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(3f, 0f);
            labelRect.offsetMax = new Vector2(-3f, 0f);
            var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = _status.font;
            label.fontSize = _status.fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = Mathf.Max(10f, _status.fontSize);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform MakeRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }
    }
}
