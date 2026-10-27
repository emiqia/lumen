using System;
using System.Collections.Generic;
using Lumen.Art;
using UnityEngine;

namespace Lumen.UI
{
    /// <summary>One row of a <see cref="MenuList"/>: a live label plus optional actions.</summary>
    public class MenuItem
    {
        public Func<string> Label;
        public Action OnConfirm;
        public Action<int> OnChange; // -1 left / +1 right, for value rows (volume, toggles...)
        public Func<bool> Muted;     // drawn darker, e.g. something you can't afford yet

        public MenuItem(Func<string> label, Action onConfirm = null, Action<int> onChange = null)
        {
            Label = label;
            OnConfirm = onConfirm;
            OnChange = onChange;
        }
    }

    /// <summary>
    /// A vertical list of pixel-text rows with a cursor. Up/Down moves, Left/Right changes value
    /// rows, Confirm activates. Used by every menu so they all look and feel the same.
    /// </summary>
    public class MenuList : MonoBehaviour
    {
        /// <summary>Vertical distance between rows; long lists can tighten it before SetItems.</summary>
        public int RowHeight { get; set; } = 13;

        readonly List<PixelText> rows = new List<PixelText>();
        List<MenuItem> items = new List<MenuItem>();
        RectTransform root;
        int selected;

        public int Selected => selected;
        public event Action<int> SelectionChanged;

        public static MenuList Create(Transform parent, Vector2 anchor, Vector2 position)
        {
            var rt = Ui.Panel(parent, "Menu", anchor, position, new Vector2(300, 10));
            var list = rt.gameObject.AddComponent<MenuList>();
            list.root = rt;
            return list;
        }

        public void SetItems(List<MenuItem> newItems, int select = 0)
        {
            foreach (var r in rows) Destroy(r.gameObject);
            rows.Clear();
            items = newItems;
            for (int i = 0; i < items.Count; i++)
                rows.Add(Ui.Text(root, "", Ui.Top, new Vector2(0, -i * RowHeight), 1, PixelText.Align.Center, Palette.UiDim, 300));
            selected = Mathf.Clamp(select, 0, Mathf.Max(0, items.Count - 1));
            Refresh();
        }

        /// <summary>Applies one frame of input. Returns true if something happened (for the blip sound).</summary>
        public bool Navigate(MenuInput input)
        {
            if (items.Count == 0) return false;
            int before = selected;
            if (input.Up) selected = (selected - 1 + items.Count) % items.Count;
            if (input.Down) selected = (selected + 1) % items.Count;
            bool acted = selected != before;
            if (acted) SelectionChanged?.Invoke(selected);

            var item = items[selected];
            if (item.OnChange != null && (input.Left || input.Right)) { item.OnChange(input.Right ? 1 : -1); acted = true; }
            else if (input.Confirm && item.OnConfirm != null) { item.OnConfirm(); acted = true; }
            else if (input.Confirm && item.OnChange != null) { item.OnChange(1); acted = true; }
            if (acted) Refresh();
            return acted;
        }

        public void Refresh()
        {
            for (int i = 0; i < rows.Count && i < items.Count; i++)
            {
                bool on = i == selected;
                string label = items[i].Label();
                rows[i].Text = on ? "> " + label + " <" : label;
                bool muted = items[i].Muted != null && items[i].Muted();
                Color c = on ? (Color)Palette.FireYellow : Palette.UiDim;
                rows[i].color = muted ? new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f, c.a) : c;
            }
        }
    }
}
