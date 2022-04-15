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

        public StringEvent onMoveStarted;
        public UnityEvent onMoveEnded;
        public UnityEvent onMultiTakeDecision;

        public Camera cam;
        private Player playerToMove;
        private Column selectedColumn;
        private List<Piece> takenPieces = new List<Piece>();
        private bool justPromoted;

        private const float PIECE_HEIGHT = 0.5f;

        public bool MoveSelectionEnabled { get; set; } = true;
        
        private void Start()
        {
            if (cam == null)
                cam = Camera.main;
        }

        public void SetPlayerToMove(Player player)
        {
            playerToMove = player;
            msg.SetCurrentTextColor(player);
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

        /// <summary>
        /// Selects column to be moved. 
        /// If there is only one possible move - it will do it;
        /// otherwise it will be just selected and possible moves will be marked. 
        /// </summary>
        /// <param name="column"> Column to move/select.</param>
        /// <returns> True, if the player can move the piece; otherwise false.</returns>
        private bool SelectColumn(Column column)
        {
            if (column.Commander.Color != playerToMove.color)
            {
                Debug.Log("Tried to select enemy column at " + column.Position);
                return false;
            }

            if (column.PossibleMoves.Count == 0)
            {
                Debug.Log("This piece on " + column.Position + " has no legal moves.");
                return false;
            }

            
            if (selectedColumn != null)
            {
                if(takenPieces.Count > 0)
                {
                    Debug.Log("You have to take with the same column as the last time.");
                    return false;
                }

                // Deselect
                board.UnmarkAll();
                if (selectedColumn == column)
                {
                    selectedColumn = null;
                    return true;
                }
            }

            selectedColumn = column;

            // If the piece has only one move - do it
            if (column.PossibleMoves.Count == 1)
            {
                // Make the move
                MakeMove(column.PossibleMoves.First());
            }
            else
            {
                markPossibleMoves(column);
            }

            return true;
        }

        private void markPossibleMoves(Column column)
        {
            board.MarkSquare(column.Position, Color.yellow);

            // Mark possible moves
            foreach (var m in column.PossibleMoves)
            {
                var s = m.Substring(m.LastIndexOf("-") + 1, 2);
                board.MarkSquare(s, Color.green);
            }
        }

        /// <summary>
        /// Moves the selected column to the specified <c>square</c>, if that move is legal. 
        /// </summary>
        /// <param name="square"> Target square.</param>
        /// <returns> True if the move can be made, false if the move is illegal.</returns>
        private bool SelectMove(string square)
        {
            if (selectedColumn == null)
            {
                Debug.Log("Column to move not selected yet!");
                return false;
            }

            board.UnmarkAll();

            string move = selectedColumn.PossibleMoves.FirstOrDefault(m => m.Contains(square));
            if(move == null)
            {
                Debug.Log("This move is not legal.");
                return false;
            }

            MakeMove(move);
            return true;
        }

        public void MakeMove(string move)
        {
            onMoveStarted.Invoke(move);

            var squares = move.Split('-');
            if (selectedColumn == null)
                selectedColumn = board.GetColumnAt(squares[0]);

            if (takenPieces.Count == 0)
            {
                msg.DisplayedMsg = "";
                msg.SetLastTextColor(playerToMove);

                selectedColumn.ZobristAll(); // XOR-out moved column
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

                var piece = selectedColumn.Commander;
                msg.DisplayedMsg += piece.Mianownik + " " + theme.MovesMsg + " " + piece.Position + " na " + targetSquare.coordinate + "\n";
                StartCoroutine(animateMove(targetSquare));
            }
        }

        private void displayKillMsg(Column takenColumn, Square targetSquare)
        {
            var killer = selectedColumn.Commander;
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
                takenPieces.Add(takenColumn.Commander);
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
            takenPieces.Add(takenColumn.Commander);
            takenColumn.Commander.MarkDark(); // Darken the taken piece

            // Can continue taking?
            if (!justPromoted) // End move on promotion 
                selectedColumn.CalcPossibleMoves(takenPieces);

            if (!justPromoted && selectedColumn.CanTake)
            {
                // Make the only possible take or mark if there are more (and wait for the player)
                if (selectedColumn.PossibleMoves.Count == 1)
                {
                    yield return new WaitForSeconds(0.1f);
                    MakeMove(selectedColumn.PossibleMoves.First());
                }
                else
                {
                    board.UnmarkAll();
                    markPossibleMoves(selectedColumn);
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
            selectedColumn.ZobristAll(); // XOR-in moved column
            board.ZobristSideToMove();

            if(takenPieces.Count == 0)
            {
                // Just move
                board.SavePositionInRepetitionHistory();
                if(!justPromoted && selectedColumn.Commander is Officer)
                    board.OfficerMovesSinceLastTake++;
                else
                    board.OfficerMovesSinceLastTake = 0;
            }
            else
            {
                // With takes
                board.ClearRepetitionHistory();
                board.OfficerMovesSinceLastTake = 0;
                takenPieces.Clear();
            }

            selectedColumn = null;
            justPromoted = false;
            onMoveEnded.Invoke();
        }

        private IEnumerator takeAnimation()
        {
            float height = takenPieces.Count * PIECE_HEIGHT;

            // Lift taken pieces (remove commanders from columns)
            foreach (var piece in takenPieces)
            {
                StartCoroutine(move(piece.gameObject, piece.transform.position.Y(piece.transform.position.y + PIECE_HEIGHT)));
            }

            // Lift the "killer"
            var p = selectedColumn.transform.position;
            p.y += height;
            yield return move(selectedColumn.gameObject, p);

            // Move taken pieces under the killer
            for(int i = 0; i<takenPieces.Count-1; i++)
            {
                var piece = takenPieces[i];
                p = selectedColumn.transform.position;
                p.y -= (i+1)*PIECE_HEIGHT;
                StartCoroutine(move(piece.gameObject, p, 0.5f + i*0.2f));
            }

            p = selectedColumn.transform.position;
            p.y = 0; //-= takenPieces.Count * pieceHeight;
            var j = takenPieces.Count - 1;
            yield return move(takenPieces[j].gameObject, p, 0.5f + j * 0.2f);

            // Reset position of the column
            var children = new List<Transform>();
            for (int i = 0; i < selectedColumn.transform.childCount; i++)
                children.Add(selectedColumn.transform.GetChild(i));

            selectedColumn.transform.DetachChildren();
            selectedColumn.transform.position = selectedColumn.transform.position.Y(0);
            foreach (var c in children)
                c.transform.parent = selectedColumn.transform;

            // Remove "darken" effect from the taken pieces
            foreach(var piece in takenPieces)
            {
                piece.UnmarkDark();
            }

            // Perform takes on the logic level and end the move
            foreach (var piece in takenPieces)
            {
                piece.Column.ZobristCommander(); // XOR-out taken piece
                selectedColumn.Take(piece.Column);
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
            yield return jump(selectedColumn.gameObject, targetSquare.transform.position.Y(0), height);

            //Debug.Log("Jumped from " + selectedColumn.Square.coordinate + " to " + targetSquare.coordinate);
            justPromoted = selectedColumn.Move(targetSquare);
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
            if (!MoveSelectionEnabled || 
                gameManager.CurrentGameState != GameManager.GameState.Turn 
                && gameManager.CurrentGameState != GameManager.GameState.PreGame)
                return;

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                var clicked = cam.GetColliderUnderMouse();
                //if (clicked != null)
                //    Debug.Log("clicked " + clicked.gameObject.name);

                if (clicked != null && clicked.transform.parent != null)
                {
                    if(clicked.gameObject.tag == "Board")
                    {
                        var square = clicked.GetComponent<Square>();
                        if (square != null)
                            squareClicked(square);
                        //else
                        //    Debug.LogError("Square component not found at " +  clicked.gameObject.name);
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
    }
}

