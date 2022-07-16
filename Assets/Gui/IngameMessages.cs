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

        private void displayColumnDescription(Column column)
        {
            gui.DrawOutline(new Rect(60, 70 + 30 * _displayedLines, 1900, 1000),
                $"Kolumna na {column.Position}:", gui.LastStyle, Color.black, column.GetActualColor());
            int i = 0;
            foreach (var p in column.Pieces)
            {
                gui.DrawOutline(new Rect(60, 100 + 30*(_displayedLines+i), 1900, 1000), $"{i+1}. {p.Mianownik}", gui.LastStyle, Color.black, p.GetActualColor());
                i++;
            }
        }
    }
}
