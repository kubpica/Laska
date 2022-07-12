using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Laska
{
    public class MoveMaker : MonoBehaviourSingleton<MoveMaker>
    {
        [GlobalComponent] private ThemeManager theme;
        [GlobalComponent] private GameManager gameManager;
        [GlobalComponent] private Board board;
        [GlobalComponent] private IngameMessages msg;
        [GlobalComponent] private CameraController cameraController;

        public StringEvent onMoveStarted;
        public UnityEvent onMoveEnded;
        public UnityEvent onMultiTakeDecision;
        public UnityEvent onColumnSelected;

        public bool MoveSelectionEnabled { get; set; } = true;

        private Column _selectedColumn;
        public Column SelectedColumn 
        {
            get => _selectedColumn;
            private set
            {
                if (_selectedColumn != value)
                {
                    _selectedColumn = value;
                    onColumnSelected.Invoke();
                }
            }
        }

        public Camera cam;
        private Player _playerToMove;
        private List<Piece> _takenPieces = new List<Piece>();
        private bool _justPromoted;
        private Color _halfSelectionColor;
        private Color _moreHalfSelectionColor;

        private const float PIECE_HEIGHT = 0.5f;

        private void Start()
        {
            if (cam == null)
                cam = Camera.main;

            _halfSelectionColor = Color.Lerp(new Color(0.855f, 0.855f, 0.855f), Color.yellow, 0.5f);
            _moreHalfSelectionColor = Color.Lerp(new Color(0.855f, 0.855f, 0.855f), Color.yellow, 0.6f);
        }

        public void SetPlayerToMove(Player player)
        {
            _playerToMove = player;
        }

        private bool SelectColumn(string square)
        {
            var column = board.GetColumnAt(square);
            if(column == null)
            {
                //Debug.Log("Tried to select empty square.");
                return false;
            }

            return SelectColumn(column);
        }

        private bool SelectColumn(Square square)
        {
            if (square.Column == null)
                return false;

            return SelectColumn(square.Column);
        }

        private bool markOnlyOneMovableColumn()
        {
            if (_playerToMove.CanOnlyOneColumnMove(out Column column))
            {
                MarkPossibleMoves(column);
                return true;
            }
            return false;
        }

        private void markColumnsThatCanTake()
        {
            if (markOnlyOneMovableColumn())
                return;

            var columns = _playerToMove.Columns;
            foreach (var c in columns)
            {
                if (c.CanTake)
                {
                    board.MarkSquare(c.Position, c.PossibleMoves.Count > 1 ? _moreHalfSelectionColor : Color.yellow);
                }
            }
        }

        private void markMovableColumns()
        {
            if (markOnlyOneMovableColumn())
                return;

            var columns = _playerToMove.Columns;
            foreach (var c in columns)
            {
                if (c.PossibleMoves.Count > 0)
                    board.MarkSquare(c.Position, _halfSelectionColor);
            }
        }

        private void markColumnsWithLegalMoves()
        {
            if (_playerToMove.CanTake)
            {
                markColumnsThatCanTake();
            }
            else
            {
                markMovableColumns();
            }
        }

        /// <summary>
        /// Selects column to be moved. 
        /// If there is only one possible move - it will do it;
        /// otherwise it will be just selected and possible moves will be marked. 
        /// </summary>
        /// <param name="column"> Column to move/select.</param>
        /// <returns> True, if the player can move the piece; otherwise false.</returns>
        private bool SelectColumn(Column column)
        {
            if (column.Commander.Color != _playerToMove.color)
            {
                Debug.Log("Tried to select enemy column at " + column.Position);
                markColumnsWithLegalMoves();
                return false;
            }

            if (column.PossibleMoves.Count == 0)
            {
                Debug.Log("This piece on " + column.Position + " has no legal moves.");
                markColumnsWithLegalMoves();
                return false;
            }

            
            if (SelectedColumn != null)
            {
                // Block deselecting the only movable column
                if(_playerToMove.CanOnlyOneColumnMove(out _))
                {
                    if (SelectedColumn == column)
                    {
                        // instead, if only one move is possible, make it
                        if (column.PossibleMoves.Count == 1)
                        {
                            board.UnmarkAll();
                            MakeMove(column.PossibleMoves.First());
                            return true;
                        }
                    }
                    Debug.Log("Only selected moves are legal, choose one.");
                    return false;
                }

                if(_takenPieces.Count > 0)
                {
                    Debug.Log("You have to take with the same column as the last time.");
                    return false;
                }

                // Deselect
                if (SelectedColumn == column)
                {
                    board.UnmarkAll();
                    SelectedColumn = null;
                    return true;
                }
            }

            board.UnmarkAll();
            SelectedColumn = column;

            // If the piece has only one move - do it
            if (column.PossibleMoves.Count == 1)
            {
                // Make the move
                MakeMove(column.PossibleMoves.First());
            }
            else
            {
                MarkPossibleMoves(column);
            }

            return true;
        }

        public void MarkPossibleMoves(Column column)
        {
            SelectedColumn = column;
            board.MarkSquare(column.Position, Color.yellow);

            // Mark possible moves
            if(column.PossibleMoves.Count > 1)
            {
                foreach (var m in column.PossibleMoves)
                {
                    var s = m.Substring(m.LastIndexOf("-") + 1, 2);
                    board.MarkSquare(s, Color.green);
                }
            }
        }

        private void deselectColumn()
        {
            if (!_playerToMove.CanOnlyOneColumnMove(out _))
            {
                SelectedColumn = null;
                board.UnmarkAll();
            }
        }

        /// <summary>
        /// Moves the selected column to the specified <c>square</c>, if that move is legal. 
        /// </summary>
        /// <param name="square"> Target square.</param>
        /// <returns> True if the move can be made, false if the move is illegal.</returns>
        private bool SelectMove(string square)
        {
            if (SelectedColumn == null)
            {
                Debug.Log("Column to move not selected yet!");
                return false;
            }

            string move = SelectedColumn.PossibleMoves.FirstOrDefault(m => m.Contains(square));
            if(move == null)
            {
                Debug.Log("This move is not legal.");
                deselectColumn();
                return false;
            }

            board.UnmarkAll();
            MakeMove(move);
            return true;
        }

        public void MakeMove(string move)
        {
            onMoveStarted.Invoke(move);

            var squares = move.Split('-');
            if (SelectedColumn == null || SelectedColumn.Position != squares[0])
                SelectedColumn = board.GetColumnAt(squares[0]);

            if (_takenPieces.Count == 0)
            {
                msg.DisplayedMsg = "";
                SelectedColumn.ZobristAll(); // XOR-out moved column
            }

            if (squares.Length > 3)
            {
                // Multi take
                StartCoroutine(animateMultiTake(squares));
            }
            else if (squares.Length == 3)
            {
                // Take
                var takenColumn = board.GetColumnAt(squares[1]);
                var targetSquare = board.GetSquareAt(squares[2]);
                StartCoroutine(animateTake(takenColumn, targetSquare));
            }
            else
            {
                // Move
                var targetSquare = board.GetSquareAt(squares[1]);

                var piece = SelectedColumn.Commander;
                msg.DisplayedMsg += piece.Mianownik + " " + theme.MovesMsg + " " + piece.Position + " na " + targetSquare.coordinate + "\n";
                StartCoroutine(animateMove(targetSquare));
            }
        }

        private void displayKillMsg(Column takenColumn, Square targetSquare)
        {
            var killer = SelectedColumn.Commander;
            var victim = takenColumn.Commander;
            msg.DisplayedMsg += killer.Mianownik + " z " + killer.Position
                + " " + theme.TakesMsg + " " + victim.Biernik + " z " + victim.Position
                + " na " + targetSquare.coordinate + "\n";
        }

        private IEnumerator animateMultiTake(string[] squares)
        {
            for (int i = 1; i<squares.Length; i+=2)
            {
                if(i != 1)
                    yield return new WaitForSeconds(0.1f);

                var takenColumn = board.GetColumnAt(squares[i]);
                var targetSquare = board.GetSquareAt(squares[i+1]);
                displayKillMsg(takenColumn, targetSquare);

                // Jump
                yield return jump(targetSquare, 1.5f + 0.5f * takenColumn.Pieces.Count);

                // Save taken pieces on the list
                _takenPieces.Add(takenColumn.Commander);
                takenColumn.Commander.MarkDark(); // Darken the taken piece
            }

            // No more takes possible
            // Animate the takes 
            yield return takeAnimation();
        }

        private IEnumerator animateTake(Column takenColumn, Square targetSquare)
        {
            displayKillMsg(takenColumn, targetSquare);

            // Jump
            yield return jump(targetSquare, 1.5f + 0.5f * takenColumn.Pieces.Count);

            // Save taken pieces on the list
            _takenPieces.Add(takenColumn.Commander);
            takenColumn.Commander.MarkDark(); // Darken the taken piece

            // Can continue taking?
            if (!_justPromoted) // End move on promotion 
                SelectedColumn.CalcPossibleMoves(_takenPieces);

            if (!_justPromoted && SelectedColumn.CanTake)
            {
                // Make the only possible take or mark if there are more (and wait for the player)
                if (SelectedColumn.PossibleMoves.Count == 1)
                {
                    yield return new WaitForSeconds(0.1f);
                    MakeMove(SelectedColumn.PossibleMoves.First());
                }
                else
                {
                    board.UnmarkAll();
                    MarkPossibleMoves(SelectedColumn);
                    onMultiTakeDecision.Invoke();
                }
            }
            else
            {
                // No more takes possible
                // Animate the takes 
                yield return takeAnimation();
            }
        }

        private IEnumerator animateMove(Square targetSquare)
        {
            // Jump
            yield return jump(targetSquare, 2);

            endMove();
        }

        private void endMove()
        {
            cameraController.MakeSureObjectCanBeSeen(SelectedColumn.Commander.gameObject);

            SelectedColumn.ZobristAll(); // XOR-in moved column
            board.ZobristSideToMove();

            if(_takenPieces.Count == 0)
            {
                // Just move
                if(!_justPromoted && SelectedColumn.Commander.IsOfficer)
                {
                    board.OfficerMovesSinceLastTake++;
                }
                else
                {
                    board.OfficerMovesSinceLastTake = 0;
                    board.ClearRepetitionHistory(); // Previous positions can't repeat when Soldier (Private) is moved
                }
            }
            else
            {
                // With takes
                board.ClearRepetitionHistory();
                board.OfficerMovesSinceLastTake = 0;
                _takenPieces.Clear();
            }
            board.SavePositionInRepetitionHistory();

            SelectedColumn = null;
            _justPromoted = false;
            onMoveEnded.Invoke();
        }

        private IEnumerator takeAnimation()
        {
            float height = _takenPieces.Count * PIECE_HEIGHT;

            // Lift taken pieces (remove commanders from columns)
            foreach (var piece in _takenPieces)
            {
                StartCoroutine(move(piece.gameObject, piece.transform.position.Y(piece.transform.position.y + PIECE_HEIGHT)));
            }

            // Lift the "killer"
            var p = SelectedColumn.transform.position;
            p.y += height;
            yield return move(SelectedColumn.gameObject, p);

            // Move taken pieces under the killer
            for(int i = 0; i<_takenPieces.Count-1; i++)
            {
                var piece = _takenPieces[i];
                p = SelectedColumn.transform.position;
                p.y -= (i+1)*PIECE_HEIGHT;
                StartCoroutine(move(piece.gameObject, p, 0.5f + i*0.2f));
            }

            p = SelectedColumn.transform.position;
            p.y = 0; //-= takenPieces.Count * pieceHeight;
            var j = _takenPieces.Count - 1;
            yield return move(_takenPieces[j].gameObject, p, 0.5f + j * 0.2f);

            // Reset position of the column
            var children = new List<Transform>();
            for (int i = 0; i < SelectedColumn.transform.childCount; i++)
                children.Add(SelectedColumn.transform.GetChild(i));

            SelectedColumn.transform.DetachChildren();
            SelectedColumn.transform.position = SelectedColumn.transform.position.Y(0);
            foreach (var c in children)
                c.transform.parent = SelectedColumn.transform;

            // Remove "darken" effect from the taken pieces
            foreach(var piece in _takenPieces)
            {
                piece.UnmarkDark();
            }

            // Perform takes on the logic level and end the move
            foreach (var piece in _takenPieces)
            {
                piece.Column.ZobristCommander(); // XOR-out taken piece
                SelectedColumn.Take(piece.Column);
            }
            endMove();
        }

        /// <summary>
        /// Lineary moves <c>go</c> to <c>target</c> position.
        /// </summary>
        private IEnumerator move(GameObject go, Vector3 target, float time = 0.5f)
        {
            Vector3 basePos = go.transform.position;
            var offset = target - basePos;
            
            for (float passed = 0.0f; passed < time;)
            {
                passed += Time.deltaTime;
                float f = passed / time;
                if (f > 1) f = 1;

                var p = basePos + offset * f;
                go.transform.position = p;

                yield return 0;
            }
        }

        private IEnumerator jump(Square targetSquare, float height)
        {
            yield return jump(SelectedColumn.gameObject, targetSquare.transform.position.Y(0), height);

            //Debug.Log("Jumped from " + selectedColumn.Square.coordinate + " to " + targetSquare.coordinate);
            _justPromoted = SelectedColumn.Move(targetSquare);
        }

        /// <summary>
        /// Animates jump anlong parabola.
        /// </summary>
        private IEnumerator jump(GameObject go, Vector3 target, float height, float time = 0.5f)
        {
            Vector3 basePos = go.transform.position;
            var direction = target - basePos;
            float distance = direction.magnitude;
            direction = direction.normalized;
            
            float x1 = 0;
            float y1 = 0;
            float x2 = distance / 2.0f;
            float y2 = height;
            float x3 = distance;
            float y3 = target.y-basePos.y;

            float denom = (x1 - x2) * (x1 - x3) * (x2 - x3);
            float A = (x3 * (y2 - y1) + x2 * (y1 - y3) + x1 * (y3 - y2)) / denom;
            float B = (x3 * x3 * (y1 - y2) + x2 * x2 * (y3 - y1) + x1 * x1 * (y2 - y3)) / denom;
            float C = (x2 * x3 * (x2 - x3) * y1 + x3 * x1 * (x3 - x1) * y2 + x1 * x2 * (x1 - x2) * y3) / denom;


            for (float passed = 0.0f; passed < time;)
            {
                passed += Time.deltaTime;
                float f = passed / time;
                if (f > 1) f = 1;

                float x = distance * f;
                float y = A * x * x + B * x + C;

                var p = basePos + direction * x;
                p.y = basePos.y + y;
                go.transform.position = p;

                yield return 0;
            }
        }

        private void squareClicked(Square square)
        {
            if(!SelectColumn(square))
            {
                SelectMove(square.coordinate);
            }
        }
        
        private void pieceClicked(Piece piece)
        {
            if (!SelectColumn(piece.Column))
            {
                SelectMove(piece.Position);
            }
        }

        private void Update()
        {
            columnInfoOnHover();

            if (!MoveSelectionEnabled || 
                gameManager.CurrentGameState != GameManager.GameState.Turn 
                && gameManager.CurrentGameState != GameManager.GameState.PreGame)
                return;

            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                var clicked = cam.GetColliderUnderMouse();

                if (clicked != null && clicked.transform.parent != null)
                {
                    if(clicked.gameObject.CompareTag("Board"))
                    {
                        var square = clicked.GetComponent<Square>();
                        if (square != null)
                        {
                            squareClicked(square);
                        }
                    }
                    else
                    {
                        var piece = clicked.GetComponent<Piece>();
                        if(piece != null)
                        {
                            pieceClicked(piece);
                        }

                        //Debug.Log("clicked piece " + piece);
                    }
                }
            }
        }

        private void columnInfoOnHover()
        {
            if (_selectedColumn != null)
                return;

            if (Input.mousePresent || Input.touchCount > 0)
            {
                var underMouse = cam.GetColliderUnderMouse();
                if (underMouse != null)
                {
                    if (underMouse.gameObject.CompareTag("Board"))
                    {
                        var square = underMouse.GetComponent<Square>();
                        if (square != null)
                        {
                            msg.SelectSquare(square);
                            return;
                        }
                    }
                    else
                    {
                        var piece = underMouse.GetComponent<Piece>();
                        if (piece != null)
                        {
                            msg.SelectColumn(piece.Column);
                            return;
                        }
                    }
                }

                if(Input.touchCount == 0)
                    msg.SelectColumn(null);
            }
        }
    }
}

