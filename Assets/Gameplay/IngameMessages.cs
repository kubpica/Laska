using UnityEngine;
using UnityEngine.SceneManagement;

namespace Laska
{
    public class IngameMessages : MonoBehaviourSingleton<IngameMessages>
    {
        [GlobalComponent] private GameManager game;

        private GUIStyle _currentStyle = new GUIStyle();
        private GUIStyle _lastStyle = new GUIStyle();
        private GUIStyle _buttonStyle = new GUIStyle();

        private int _level = 1;
        private float _widthScale;
        private float _heightScale;

        public string DisplayedMsg { get; set; }

        private void Start()
        {
            _currentStyle.fontStyle = FontStyle.Bold;
            _currentStyle.normal.textColor = Color.green;
            _currentStyle.fontSize = 13;
            _lastStyle.fontStyle = FontStyle.Bold;
            _lastStyle.normal.textColor = Color.green;
            _lastStyle.fontSize = 13;

            _buttonStyle = new GUIStyle("button");
            _buttonStyle.fontSize = 13;
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

        private Rect scaleRect(float x, float y, float width, float height)
        {
            return new Rect(x * _widthScale, y * _heightScale, width * _widthScale, height * _heightScale);
        }

        private void OnGUI()
        {
            _widthScale = Mathf.Max(Screen.width / 1917f, 0.5f);
            _heightScale = Mathf.Max(Screen.height / 908f, 0.5f);

            _currentStyle.fontSize = (int)(21 * _heightScale);
            _lastStyle.fontSize = (int)(21 * _heightScale);
            _buttonStyle.fontSize = (int)(26 * _heightScale);

            if (GUI.Button(scaleRect(Screen.width / _widthScale - 340, 10, 300, 80), _level == 7 ? "Bot wyłączony" : (_level == 0 ? "Poziom X" : "Poziom " + _level), _buttonStyle))
            {
                var blackPlayer = GameManager.Instance.GetPlayer('b');
                var ai = blackPlayer.AI;
                _level++;
                _level %= 8;
                if (_level == 7)
                {
                    blackPlayer.isAI = false;
                }
                else if (_level > 0)
                {
                    blackPlayer.isAI = true;
                    ai.cfg.limitDeepeningDepth = true;
                    ai.cfg.searchDepth = _level;
                }
                else
                {
                    blackPlayer.isAI = true;
                    ai.cfg.limitDeepeningDepth = false;
                }
            }

            if (GUI.Button(scaleRect(Screen.width / _widthScale - 340, 100, 300, 80), "Nowa gra", _buttonStyle))
            {
                PiecesManager.TempMoves = false;
                SceneManager.LoadScene("Laska");
            }

            if (GUI.Button(scaleRect(Screen.width / _widthScale - 340, 190, 300, 80), "Obróć", _buttonStyle))
            {
                CameraController.Instance.ChangePerspective();
            }

            //GUI.Label(new Rect(Screen.width-126, 10, 200, 20), "Tiktok: @warcoins", _currentStyle);

            string player = game.ActivePlayer.color == 'b' ? "czerwonego" : "zielonego";
            if (game.Mate)
            {
                GUI.Label(scaleRect(60, 10, 200, 20), "Pat-mat! Wygrana gracza " + player, _currentStyle);
                return;
            }
            else if (game.DrawByRepetition)
            {
                GUI.Label(scaleRect(60, 10, 200, 20), "Remis przez powtórzenie!", _currentStyle);
                return;
            }
            else if (game.DrawByFiftyMoveRule)
            {
                GUI.Label(scaleRect(60, 10, 200, 20), "Remis przez 50 ruchów bez bicia!", _currentStyle);
                return;
            }

            GUI.Label(scaleRect(60, 10, 200, 20), "Ruch gracza " + player, _currentStyle);
            if (DisplayedMsg != null)
            {
                var msg = DisplayedMsg;

                DrawOutline(scaleRect(60, 40, 1900, 1000), msg, _lastStyle, Color.black, _lastStyle.normal.textColor);
            }
        }
    }
}
