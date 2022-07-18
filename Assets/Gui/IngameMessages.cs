using System.Linq;
using UnityEngine;

namespace Laska
{
    public class IngameMessages : MonoBehaviourSingleton<IngameMessages>
    {
        [GlobalComponent] private GameManager game;
        [GlobalComponent] private MoveMaker moveMaker;
        [GlobalComponent] private GuiScaler gui;


        private Camera _cam;
        private Square _selectedSquare;

        private float _cachedEval;
        private int _displayedLines;

        private string _displayedMsg;
        public string DisplayedMsg 
        {
            get => _displayedMsg;
            set
            {
                _displayedMsg = value;
                if (!string.IsNullOrEmpty(_displayedMsg))
                {
                    _displayedLines = Mathf.Max(1, _displayedMsg.Count(c => c == '\n'));
                }
                else
                {
                    _displayedLines = 0;
                }
            }
        }

        public bool DisplayEval { get; set;}

        private void Start()
        {
            _cam = CameraController.Instance.Camera;
            moveMaker.onColumnSelected.AddListener(() => SelectColumn(moveMaker.SelectedColumn));
        }

        private void Update()
        {
            columnInfoOnHover();   
        }

        private void columnInfoOnHover()
        {
            if (moveMaker.SelectedColumn != null)
                return;

            if (Input.mousePresent || Input.touchCount > 0)
            {
                var underMouse = _cam.GetColliderUnderMouse();
                if (underMouse != null)
                {
                    if (underMouse.gameObject.CompareTag("Board"))
                    {
                        var square = underMouse.GetComponent<Square>();
                        if (square != null)
                        {
                            SelectSquare(square);
                            return;
                        }
                    }
                    else
                    {
                        var piece = underMouse.GetComponent<Piece>();
                        if (piece != null)
                        {
                            SelectColumn(piece.Column);
                            return;
                        }
                    }
                }

                if (Input.touchCount == 0)
                    SelectColumn(null);
            }
        }

        public void SelectColumn(Column column)
        {
            _selectedSquare = column == null ? null : column.Square;
        }

        public void SelectSquare(Square square)
        {
            _selectedSquare = square.draughtsNotationIndex == 0 ? null : square;
        }

        private void OnGUI()
        {
            displaySelectedMsg();
            string player = game.ActivePlayer.color == 'b' ? "czerwonego" : "zielonego";
            if (game.Mate)
            {
                gui.LabelTopLeft(new Rect(60, 10, 200, 20), "Pat-mat! Wygrana gracza " + player);
                gameOver();
                return;
            }
            else if (game.DrawByRepetition)
            {
                gui.LabelTopLeft(new Rect(60, 10, 200, 20), "Remis przez powtórzenie!");
                gameOver();
                return;
            }
            else if (game.DrawByFiftyMoveRule)
            {
                gui.LabelTopLeft(new Rect(60, 10, 200, 20), "Remis przez 50 ruchów bez bicia!");
                gameOver();
                return;
            }

            gui.LabelTopLeft(new Rect(60, 10, 200, 20), "Ruch gracza " + player);
            if (DisplayedMsg != null)
            {
                gui.DrawOutline(new Rect(60, 40, 1900, 1000), DisplayedMsg, gui.LastStyle, Color.black, gui.LastStyle.normal.textColor);
            }
        }

        private void gameOver()
        {
            gui.DrawOutline(new Rect(60, 40, 1900, 1000), "Koniec gry! :)", gui.CurrentStyle, Color.black, gui.CurrentStyle.normal.textColor);
        }

        private void displaySelectedMsg()
        {
            if (DisplayEval && _selectedSquare == null)
            {
                displayPositionEval();
                return;
            }

            if (PiecesManager.TempMoves || _selectedSquare == null || game.CurrentGameState == GameManager.GameState.TurnResults)
                return;

            var column = _selectedSquare.Column;
            if (column == null)
                displaySquareDescription(_selectedSquare);
            else
                displayColumnDescription(column);
        }

        private void displaySquareDescription(Square square)
        {
            gui.DrawOutline(new Rect(60, 70 + 30 * _displayedLines, 1900, 1000),
                $"Pole {square.coordinate}.", gui.LastStyle, Color.black, gui.LightGray);
        }

        private string formatEval(float eval, char color)
        {
            if (color != game.ActivePlayer.color)
                eval *= -1;

            string msg = eval > 0 ? "+" : "";
            return msg + string.Format("{0:0.##}", eval);
        }

        public void UpdateEval()
        {
            var player = game.ActivePlayer;
            _cachedEval = player.AI.EvaluatePosition(player) / 10000f;
        }

        private void displayPositionEval()
        {
            var msg = $"Ocena pozycji: {formatEval(_cachedEval, game.ActivePlayer.color)}";
            gui.DrawOutline(new Rect(60, 70 + 30 * _displayedLines, 1900, 1000),
                   msg, gui.LastStyle, Color.black, game.ActivePlayer.GetActualColor());
        }

        private void displayColumnDescription(Column column)
        {
            var msg = $"Kolumna na {column.Position}:";
            if (DisplayEval)
            {
                msg += $" ({formatEval(column.Value / 3.608963f, column.Commander.Color)})";
            }

            gui.DrawOutline(new Rect(60, 70 + 30 * _displayedLines, 1900, 1000),
                msg, gui.LastStyle, Color.black, column.GetActualColor());
            int i = 0;
            foreach (var p in column.Pieces)
            {
                gui.DrawOutline(new Rect(60, 100 + 30*(_displayedLines+i), 1900, 1000), $"{i+1}. {p.Mianownik}", gui.LastStyle, Color.black, p.GetActualColor());
                i++;
            }
        }
    }
}
