﻿using UnityEngine;

namespace Laska
{
    public class GuiScaler : MonoBehaviourSingleton<GuiScaler>
    {
        public GUIStyle CurrentStyle { get; private set; } = new GUIStyle();
        public GUIStyle LastStyle { get; private set; } = new GUIStyle();
        public GUIStyle ButtonStyle { get; private set; } = new GUIStyle();

        public float WidthScale { get; private set; }
        public float HeightScale { get; private set; }

        private void Start()
        {
            CurrentStyle.fontStyle = FontStyle.Bold;
            CurrentStyle.normal.textColor = Color.green;
            CurrentStyle.fontSize = 13;
            LastStyle.fontStyle = FontStyle.Bold;
            LastStyle.normal.textColor = Color.green;
            LastStyle.fontSize = 13;

            ButtonStyle = new GUIStyle("button");
            ButtonStyle.fontSize = 13;
        }

        public void DrawOutline(Rect pos, string text, GUIStyle style, Color outColor, Color inColor)
        {
            pos = ScaleRect(pos);

            var backupColor = style.normal.textColor;
            style.normal.textColor = outColor;
            pos.x--;
            GUI.Label(pos, text, style);
            pos.x += 2;
            GUI.Label(pos, text, style);
            pos.x--;
            pos.y--;
            GUI.Label(pos, text, style);
            pos.y += 2;
            GUI.Label(pos, text, style);
            pos.y--;
            style.normal.textColor = inColor;
            GUI.Label(pos, text, style);
            style.normal.textColor = backupColor;
        }

        public void SetCurrentTextColor(Player player)
        {
            SetCurrentTextColor(player.GetActualColor());
        }

        public void SetCurrentTextColor(Color color)
        {
            CurrentStyle.normal.textColor = color;
        }

        public void SetLastTextColor(Player player)
        {
            SetLastTextColor(player.GetActualColor());
        }

        public void SetLastTextColor(Color color)
        {
            LastStyle.normal.textColor = color;
        }

        public Rect ScaleRect(Rect r) => ScaleRect(r.x, r.y, r.width, r.height);

        public Rect ScaleRect(float x, float y, float width, float height)
        {
            return new Rect(x * WidthScale, y * HeightScale, width * WidthScale, height * HeightScale);
        }

        public bool ButtonTopRight(Rect r, string text)
        {
            return GUI.Button(ScaleRect(Screen.width / WidthScale - r.x, r.y, r.width, r.height), text, ButtonStyle);
        }

        public bool ButtonBottomRight(Rect r, string text)
        {
            return GUI.Button(ScaleRect(Screen.width / WidthScale - r.x, Screen.height / HeightScale - r.y, r.width, r.height), text, ButtonStyle);
        }

        public void LabelTopLeft(Rect r, string text)
        {
            GUI.Label(ScaleRect(r.x, r.y, r.width, r.height), text, CurrentStyle);
        }

        private void OnGUI()
        {
            WidthScale = Mathf.Max(Screen.width / 1917f, 0.25f);
            HeightScale = Mathf.Max(Screen.height / 908f, 0.25f);

            CurrentStyle.fontSize = (int)(21 * HeightScale);
            LastStyle.fontSize = (int)(21 * HeightScale);
            ButtonStyle.fontSize = (int)(26 * WidthScale);
        }
    }
}
