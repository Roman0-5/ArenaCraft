using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaCraft
{
    /// <summary>
    /// Lets the full-screen menus (Main Menu, Options, Victory) be driven from the
    /// keyboard by either player as well as the mouse. Up/W or Down/S move the
    /// highlight; Enter/Space/F/Right-Shift activate it. Buttons remain clickable
    /// via the EventSystem too.
    /// </summary>
    public class MenuNavigator : MonoBehaviour
    {
        private readonly List<Button> _buttons = new List<Button>();
        private int _index;
        private bool _active;

        public void SetButtons(IEnumerable<Button> buttons)
        {
            _buttons.Clear();
            _buttons.AddRange(buttons);
            _index = 0;
            Highlight();
        }

        public void SetActive(bool on)
        {
            _active = on;
            if (on) { _index = 0; Highlight(); }
        }

        private void Update()
        {
            if (!_active || _buttons.Count == 0) return;

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                _index = (_index - 1 + _buttons.Count) % _buttons.Count;
                Highlight();
            }
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                _index = (_index + 1) % _buttons.Count;
                Highlight();
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.F) ||
                Input.GetKeyDown(KeyCode.RightShift))
            {
                if (_index >= 0 && _index < _buttons.Count && _buttons[_index] != null)
                    _buttons[_index].onClick.Invoke();
            }
        }

        private void Highlight()
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                if (_buttons[i] == null) continue;
                var img = _buttons[i].GetComponent<Image>();
                if (img == null) continue;
                var baseCol = _buttons[i].colors.normalColor;
                img.color = (i == _index) ? Color.Lerp(baseCol, Color.white, 0.35f) : baseCol;
            }
        }
    }
}
