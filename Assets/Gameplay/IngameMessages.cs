using System.Linq;
using UnityEngine;

namespace Laska
{
    public class IngameMessages : MonoBehaviourSingleton<IngameMessages>
    {
        [GlobalComponent] private GameManager game;
        [GlobalComponent] private MoveMaker moveMaker;
        [GlobalComponent] private GuiScaler gui;

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
                    _displayedLines = _displayedMsg.Count(c => c == '\n');
                }
                else
                {
                    _displayedLines = 0;
                }
            }
        }

        private void Start()
        {
            moveMaker.onColumnSelected.AddListener(() => SelectColumn(moveMaker.SelectedColumn));
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
            string player = game.ActivePlayer.color == 'b' ? "czerwonego" : "zielonego";
            if (game.Mate)
            {
                gui.LabelTopLeft(new Rect(60, 10, 200, 20), "Pat-mat! Wygrana gracza " + player);
                return;
            }
            else if (game.DrawByRepetition)
            {
                gui.LabelTopLeft(new Rect(60, 10, 200, 20), "Remis przez powtórzenie!");
                return;
            }
            else if (game.DrawByFiftyMoveRule)
            {
                gui.LabelTopLeft(new Rect(60, 10, 200, 20), "Remis przez 50 ruchów bez bicia!");
                return;
            }

            gui.LabelTopLeft(new Rect(60, 10, 200, 20), "Ruch gracza " + player);
            if (DisplayedMsg != null)
            {
                gui.DrawOutline(new Rect(60, 40, 1900, 1000), DisplayedMsg, gui.LastStyle, Color.black, gui.LastStyle.normal.textColor);
            }
            displaySelectedMsg();
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
                $"Pole {square.coordinate}.", gui.LastStyle, Color.black, new Color(0.855f, 0.855f, 0.855f));
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
