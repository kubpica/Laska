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
            gui.LabelTopLeft(new Rect(60, 10, 200, 20), "Laska: 3D Checkers v1.0.1 by kubpica");
            var msg = "TikTok: @warcoins\nSfx: el-boss, fachii, gronkjaer, mlaudio (freesound.org)\n" +
                      "PP: github.com/kubpica/Laska (PRIVACY_POLICY.md)\n" +
                      "We do not collect any personal data :)\n" +
                      "Please consider rating this app on Google Play!";
            gui.DrawOutline(new Rect(60, 40, 1900, 2000), msg, gui.LastStyle, Color.black, Color.red);

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
