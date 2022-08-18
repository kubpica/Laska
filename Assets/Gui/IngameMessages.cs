using System.Linq;
using UnityEngine;

namespace Laska
{
    public class IngameMessages : MonoBehaviourSingleton<IngameMessages>
    {
        [GlobalComponent] private GameManager game;
        [GlobalComponent] private MoveMaker moveMaker;
        [GlobalComponent] private GuiScaler gui;
        [GlobalComponent] private ThemeManager theme;
        [GlobalComponent] private AudioManager sfx;

        private Language Language => LanguageManager.Language;

        private Camera _cam;
        private Square _selectedSquare;
        private Column _selectedColumn;

        private float _cachedEval;
        private int _displayedLines;
        private int _aiThinkingTimer;
        private string _aiThinkingMsg;

        private bool IsWhite => game.ActivePlayer.color == 'w';
        private System.Diagnostics.Stopwatch AIStopwatch => game.ActivePlayer.AI.SearchStopwatch;

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

        public bool DisplayEval { get; set; }
        

        private void Start()
        {
            _cam = CameraController.Instance.Camera;
            moveMaker.onColumnSelected.AddListener(() =>
            {
                if (!PiecesManager.TempMoves && GameManager.GetAIMode() != GameManager.AIMode.AIVsAI
                    && !game.ActivePlayer.isAI)
                    SelectColumn(moveMaker.SelectedColumn);
            });
            moveMaker.onMoveEnded.AddListener(() => _aiThinkingTimer = 0);
        }

        private void Update()
        {
            columnInfoOnHover();
            whenAIThinking();
        }

        private void columnInfoOnHover()
        {
            if (moveMaker.SelectedColumn != null && GameManager.GetAIMode() != GameManager.AIMode.AIVsAI)
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
                            if (!PiecesManager.TempMoves)
                                SelectColumn(piece.Column);
                            return;
                        }
                    }
                }

                if (Input.touchCount == 0)
                    SelectColumn(null);
            }
        }

        private void whenAIThinking()
        {
            if (!game.IsAIThinking)
                return;

            if (AIStopwatch.ElapsedMilliseconds > _aiThinkingTimer * 1000)
            {
                _aiThinkingTimer++;
                _aiThinkingMsg = Language.aiIsThinking;
                if (_aiThinkingTimer % 3 > 0)
                    _aiThinkingMsg += _aiThinkingTimer % 3 == 1 ? "." : "..";
                if (_aiThinkingTimer > 3)
                    _aiThinkingMsg += "(" + (_aiThinkingTimer-1) + ")";
            }
        }

        public void SelectColumn(Column column)
        {
            var square = column == null ? null : column.Square;
            if (_selectedSquare == square)
                return;

            _selectedSquare = square;
            _selectedColumn = column;
            if (_selectedSquare != null)
                sfx.PlayAtPoint("Hover", column.Commander.transform.position);
        }

        public void SelectSquare(Square square)
        {
            square = square.draughtsNotationIndex == 0 ? null : square;
            if (_selectedSquare == square)
                return;

            _selectedSquare = square;
            if (_selectedSquare != null)
            {
                _selectedColumn = square.Column;
                sfx.PlayAtPoint("Hover", square.transform.position);
            }
        }

        private void OnGUI()
        {
            displaySelectedMsg();

            if (game.ActivePlayer == null)
                return;

            if (game.Mate)
            {
                gui.LabelTopLeft(new Rect(60, 10, 200, 20), $"{Language.mate} {(IsWhite ? Language.greenWins : Language.redWins)}");
                gameOver();
            }
            else if (game.DrawByRepetition)
            {
                gui.LabelTopLeft(new Rect(60, 10, 200, 20), Language.drawByRepetition);
                gameOver();
            }
            else if (game.DrawByFiftyMoveRule)
            {
                gui.LabelTopLeft(new Rect(60, 10, 200, 20), Language.drawBy50MoveRule);
                gameOver();
            }
            else
            {
                string msg = IsWhite ? Language.greenPlayerToMove : Language.redPlayerToMove;
                if (game.IsAIThinking && _aiThinkingTimer > 1)
                    msg += ". " + _aiThinkingMsg;
                gui.LabelTopLeft(new Rect(60, 10, 200, 20), msg);
                displayMsg();
            }
        }

        private void gameOver()
        {
            if (DisplayedMsg.StartsWith(Language.reviewFailed) || DisplayedMsg.EndsWith(Language.areYouSureExit))
            {
                displayMsg();
            }
            else
            {
                gui.DrawOutline(new Rect(60, 40, 1900, 1000), theme.GameOver, gui.CurrentStyle, Color.black, gui.CurrentStyle.normal.textColor);
            }
        }

        private void displayMsg()
        {
            if (DisplayedMsg != null)
            {
                gui.DrawOutline(new Rect(60, 40, 1900, 1000), DisplayedMsg, gui.LastStyle, Color.black, gui.LastStyle.normal.textColor);
            }
        }

        private void displaySelectedMsg()
        {
            if (DisplayEval && _selectedSquare == null)
            {
                displayPositionEval();
                return;
            }

            if (_selectedSquare == null)
                return;

            if (PiecesManager.TempMoves)
            {
                displaySquareDescription(_selectedSquare);
                return;
            }

            if(game.CurrentGameState == GameManager.GameState.TurnResults)
            {
                if (!game.ActivePlayer.isAI)
                    return;

                if (_selectedSquare.IsEmpty && _selectedColumn != null && _selectedColumn.Pieces.Count > 0)
                {
                    displayColumnDescription(_selectedColumn);
                }
            }

            var column = _selectedSquare.Column;
            if (column == null)
                displaySquareDescription(_selectedSquare);
            else
                displayColumnDescription(column);
        }

        private void displaySquareDescription(Square square)
        {
            if(theme.IsStinkyCheese && square.coordinate == "d4")
            {
                gui.DrawOutline(new Rect(60, 70 + 30 * _displayedLines, 1900, 1000),
                    "D4 ŚMIERDZĄCY SER!", gui.LastStyle, Color.black, Color.yellow);
                return;
            }

            gui.DrawOutline(new Rect(60, 70 + 30 * _displayedLines, 1900, 1000),
                $"{Language.square} {square.coordinate}.", gui.LastStyle, Color.black, gui.LightGray);
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
            var msg = $"{theme.PositionEval} {formatEval(_cachedEval, game.ActivePlayer.color)}";
            gui.DrawOutline(new Rect(60, 70 + 30 * _displayedLines, 1900, 1000),
                   msg, gui.LastStyle, Color.black, game.ActivePlayer.GetActualColor());
        }

        private void displayColumnDescription(Column column)
        {
            var msg = $"{theme.Column} {Language.on} {column.Position}:";
            if (DisplayEval)
            {
                msg += $" ({formatEval(column.Value / 3.608963f, column.Commander.Color)})";
            }

            bool isStinky = theme.IsStinkyCheese && column.Square.coordinate == "d4";
            if (isStinky)
            {
                msg += " (śmierdzi)";
            }
            Color color = isStinky ? Color.yellow : column.GetActualColor();
            
            gui.DrawOutline(new Rect(60, 70 + 30 * _displayedLines, 1900, 1000),
                msg, gui.LastStyle, Color.black, color);
            int i = 0;
            foreach (var p in column.Pieces)
            {
                gui.DrawOutline(new Rect(60, 100 + 30*(_displayedLines+i), 1900, 1000), $"{i+1}. {p.Mianownik}", gui.LastStyle, Color.black, p.GetActualColor());
                i++;
            }
        }
    }
}
