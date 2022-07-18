using System;
using UnityEngine;

namespace Laska
{
    public class LanguageMenu : MonoBehaviourExtended
    {
        [GlobalComponent] private MenusManager menus;
        [GlobalComponent] private GuiScaler gui;

        public Language polish;
        public Language english;
        public Theme zymskieTheme;

        private void OnGUI()
        {
            langButton(0, "Wybierz język:", "polski", () => LanguageManager.Language = polish);
            langButton(1, "Choose language:", "english", () => LanguageManager.Language = english);
            langButton(2, "Żymianie naprzód:", "żymski", () => ThemeManager.Instance.theme = zymskieTheme);
        }

        private void langButton(int i, string chooseLang, string lang, Action onClick)
        {
            float x = 167 + 639 * i;
            gui.DrawOutline(new Rect(x, 340, 305, 34), chooseLang, gui.LastStyle, Color.black, Color.green);
            if (gui.ButtonTopLeft(new Rect(x, 374, 305, 80), lang, Color.yellow))
            {
                onClick();
                menus.ExitLanguageMenu();
            }
        }
    }
}
