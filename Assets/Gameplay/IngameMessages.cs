using UnityEngine;

namespace Laska
{
    public class IngameMessages : MonoBehaviourSingleton<IngameMessages>
    {
        [GlobalComponent] private GameManager game;

        private GUIStyle _currentStyle = new GUIStyle();
        private GUIStyle _lastStyle = new GUIStyle();

        public string DisplayedMsg { get; set; }

        private void Start()
        {
            _currentStyle.fontStyle = FontStyle.Bold;
            _currentStyle.normal.textColor = Color.green;
            _lastStyle.fontStyle = FontStyle.Bold;
            _lastStyle.normal.textColor = Color.green;
        }

        public void SetCurrentTextColor(Player player)
        {
            SetCurrentTextColor(player.color == 'b' ? Color.red : Color.green);
        }

        public void SetCurrentTextColor(Color color)
        {
            _currentStyle.normal.textColor = color;
        }

        public void SetLastTextColor(Player player)
        {
            SetLastTextColor(player.color == 'b' ? Color.red : Color.green);
        }

        public void SetLastTextColor(Color color)
        {
            _lastStyle.normal.textColor = color;
        }

        public static void DrawOutline(Rect pos, string text, GUIStyle style, Color outColor, Color inColor)
        {
            //GUIStyle backupStyle = style;
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
            //style = backupStyle;
        }

        private void OnGUI()
        {
            //GUI.Label(new Rect(Screen.width-126, 10, 200, 20), "Tiktok: @warcoins", currentStyle);
            string player = game.ActivePlayer.color == 'b' ? "czerwonego" : "zielonego";
            if (game.Mate)
            {
                GUI.Label(new Rect(10, 10, 200, 20), "Pat-mat! Wygrana gracza " + player, _currentStyle);
                return;
            }
            else if (game.DrawByRepetition)
            {
                GUI.Label(new Rect(10, 10, 200, 20), "Remis przez powtórzenie!", _currentStyle);
                return;
            }
            else if (game.DrawByFiftyMoveRule)
            {
                GUI.Label(new Rect(10, 10, 200, 20), "Remis przez 50 ruchów bez bicia!", _currentStyle);
                return;
            }

            GUI.Label(new Rect(10, 10, 200, 20), "Ruch gracza " + player, _currentStyle);
            if (DisplayedMsg != null)
            {
                var msg = DisplayedMsg;

                DrawOutline(new Rect(10, 30, 1900, 1000), msg, _lastStyle, Color.black, _lastStyle.normal.textColor);
            }
        }
    }
}
