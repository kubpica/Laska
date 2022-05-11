using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laska
{
    public class LaskaAI : MonoBehaviourExtended
    {
        [GlobalComponent] private Board board;
        [GlobalComponent] private GameManager gameManager;
        [Component] private MoveOrdering moveOrdering;

        public int searchDepth;
        public bool forcedSequencesAsOneMove;
        public bool searchAllTakes;
        public bool searchUnsafePositions;
        public bool antyZugzwang;
        public bool evalColumnsValue;
        public float officerValue = 10.296f;
        public float officerCaptivesShare = 0.208f;
        public float soldierValue = 4.08477f; //4.160353f; //4.08477f; //6.94963f; //5.610073f; //4.456758f;
        public float soldierCaptivesShare = 0.523585f; //0.5231462f; //0.523585f; //0.2609079f; //0.3636922f; //0.3041386f;
        public int pointsPerOwnedColumn = 10000;
        public bool evalColumnsStrength;
        public int pointsPerExtraColumnStrength = 10000;
        public bool evalSpace;
        public bool orderMoves;

        private const int ACTIVE_WIN = 1000000;
        private const int INACTIVE_WIN = -1000000;
        private const int DRAW = -1;

        private HashSet<ulong> _visitedNonTakePositions = new HashSet<ulong>(); // Zobrist keys
        private bool _isSearchingZugzwang;
        private HashSet<string> _cachedSafeSquares = new HashSet<string>();

        public float EvaluatePosition(Player playerToMove)
        {
            var activeColumns = gameManager.ActivePlayer.GetOwnedColums();
            var inactiveColumns = gameManager.InactivePlayer.GetOwnedColums();

            // Check for mate
            int activeColumnsCount = activeColumns.Count();
            if (activeColumnsCount == 0)
            {
                return INACTIVE_WIN;
            }

            int inactiveColumnsCount = inactiveColumns.Count();
            if (inactiveColumnsCount == 0)
            {
                return ACTIVE_WIN;
            }

            // Check for stalemate (We can skip it as we check it in the minimax func)
            //if (!playerToMove.HasNewPossibleMoves())
            //    return playerToMove == gameManager.ActivePlayer ? INACTIVE_WIN : ACTIVE_WIN;

            float activeScore = 0;
            
            var activePieceDiff = activeColumnsCount - inactiveColumnsCount;
            activeScore = activePieceDiff * pointsPerOwnedColumn;

            if (evalColumnsValue)
            {
                foreach (var c in activeColumns)
                    activeScore += c.Value * 2408.118596f;

                foreach (var c in inactiveColumns)
                    activeScore -= c.Value * 2408.118596f;
            }

            if (evalColumnsStrength)
            {
                foreach (var c in activeColumns)
                    activeScore += (c.Strength - 1) * pointsPerExtraColumnStrength;

                foreach (var c in inactiveColumns)
                    activeScore -= (c.Strength - 1) * pointsPerExtraColumnStrength;
            }

            if (evalSpace)
            {
                _cachedSafeSquares.Clear();
                foreach (var c in activeColumns)
                    activeScore += calcAccessibleSquares(c);

                _cachedSafeSquares.Clear();
                foreach (var c in inactiveColumns)
                    activeScore -= calcAccessibleSquares(c);
            }
            else if (activeScore > 20000) //activePieceDiff > 2
            {
                activeScore += calcDistanceScore();
            }

            return activeScore;
        }

        /// <summary>
        /// Visits accessible squares recursively and returns number of safe ones.
        /// </summary>
        private int calcAccessibleSquares(Column column)
        {
            var visitedSquares = new HashSet<Square>();
            visitAccessibleSquares(column.MovementDirections, column.Square, false);
            return visitedSquares.Count;

            void visitAccessibleSquares(List<string> movementDirections, Square square, bool useCache)
            {
                board.GetSquareIds(square.coordinate, out int file, out int rank);

                // Get squares accessible from current square
                foreach (var dir in movementDirections)
                {
                    int dirX = dir[0] == '-' ? -1 : 1;
                    int dirY = dir[1] == '-' ? -1 : 1;

                    // Visit next square recursively
                    try
                    {
                        var s = board.GetSquareAt(file + 1 * dirX, rank + 1 * dirY);
                        if (s.IsEmpty && !visitedSquares.Contains(s) && isSquareSafe(s.coordinate, column, useCache))
                        {
                            visitedSquares.Add(s);
                            visitAccessibleSquares(movementDirections, s, true);
                        }
                    }
                    catch { }
                }
            }
        }

        private bool isPositionSafe(Player playerToMove, List<string> moves)
        {
            return !hasAnyUnsafePiece(playerToMove) && hasAnySafeMove(playerToMove, moves);
        }

        private bool hasAnyUnsafePiece(Player playerToMove)
        {
            foreach(var column in playerToMove.Columns)
            {
                if (!isSquareSafe(column.Position, column))
                    return true;
            }
            return false;
        }

        private bool hasAnySafeMove(Player playerToMove, List<string> moves)
        {
            if (playerToMove.CanTake)
                return false;

            foreach (var move in moves)
            {
                var squares = move.Split('-');
                if (isSquareSafe(squares[1], board.GetColumnAt(squares[0])))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Square is safe for <c>columnAtRisk</c> when being on that square is not forcing oponent to take. 
        /// </summary>
        private bool isSquareSafe(string square, Column columnAtRisk, bool useCache = false)
        {
            if (useCache && _cachedSafeSquares.Contains(square))
                return true;

            // The column is near board border so it's safe
            if (square[1] == '1' || square[1] == '7' || square[0] == 'a' || square[0] == 'g')
                return true;

            board.GetSquareIds(square, out int file, out int rank);
            bool isSafe = isSquareDiagonalSafe(1) && isSquareDiagonalSafe(-1);
            if(isSafe && useCache)
            {
                _cachedSafeSquares.Add(square);
            }
            return isSafe;

            bool isSquareDiagonalSafe(int dir)
            {
                var upperColumn = board.GetColumnAt(file - 1 * dir, rank + 1);
                var lowerColumn = board.GetColumnAt(file + 1 * dir, rank - 1);
                if (upperColumn == columnAtRisk)
                    upperColumn = null;
                else if (lowerColumn == columnAtRisk)
                    lowerColumn = null;

                if ((upperColumn == null) != (lowerColumn == null)) // One of columns have space to take
                {
                    Column attacker = upperColumn == null ? lowerColumn : upperColumn;
                    if (attacker.Commander.Color == columnAtRisk.Commander.Color)
                        return true;

                    int attackerFile;
                    int attackerRank;
                    if (attacker == lowerColumn)
                    {
                        attackerFile = file + 1 * dir;
                        attackerRank = rank - 1;
                    }
                    else
                    {
                        attackerFile = file - 1 * dir;
                        attackerRank = rank + 1;
                    }

                    string takeDirection = attackerFile > file ? "-" : "+";
                    takeDirection += attackerRank > rank ? "-" : "+";
                    if (attacker.MovementDirections.Contains(takeDirection))
                    {
                        return false;
                    }
                }
                // Either no attacking columns or no space to take - so it's safe
                return true;
            }
        }

        private int calcDistanceScore()
        {
            int score = 0;
            foreach (var ac in gameManager.ActivePlayer.pieces.Where(p => p.IsFree))
            {
                foreach (var ic in gameManager.InactivePlayer.pieces.Where(p => p.IsFree))
                {
                    score += (int)Mathf.Pow(6 - board.CalcDistance(ac.Position, ic.Position), 2);
                }
            }
            return score;
        }

        private Column makeMove(string move, out Stack<Square> takenSquares, out Square previousSquare, out bool promotion)
        {
            var squares = move.Split('-');
            Column movedColumn = board.GetColumnAt(squares[0]);
            previousSquare = movedColumn.Square;
            movedColumn.ZobristAll(); // XOR-out column from old square

            Square targetSquare;
            if (squares.Length >= 3)
            {
                // Take
                takenSquares = new Stack<Square>();
                targetSquare = board.GetSquareAt(squares[squares.Length-1]);

                for(int i = 1; i<squares.Length; i += 2)
                {
                    var takenColumn = board.GetColumnAt(squares[i]);
                    takenSquares.Push(takenColumn.Square);
                    takenColumn.ZobristCommander(); // XOR-out taken piece
                    movedColumn.Take(takenColumn);
                }
            }
            else
            {
                // Move
                takenSquares = null;
                targetSquare = board.GetSquareAt(squares[1]);
            }
            promotion = movedColumn.Move(targetSquare);

            movedColumn.ZobristAll(); // XOR-in column on new square (with new pieces if take)
            board.ZobristSideToMove();

            return movedColumn;
        }

        private void unmakeMove(Column movedColumn, Stack<Square> takenSquares, Square previousSquare, bool demote)
        {
            board.ZobristSideToMove();
            movedColumn.ZobristAll(); // XOR-out column from current square

            if (takenSquares != null)
            {
                while (takenSquares.Count > 0)
                {
                    var takenSquare = takenSquares.Pop();
                    movedColumn.Untake(takenSquare);
                    takenSquare.Column.ZobristCommander(); // XOR-in restored piece
                }
            }

            movedColumn.Move(previousSquare);

            if (demote)
                movedColumn.Demote();

            movedColumn.ZobristAll(); // XOR-in column on previous square (with previous pieces)
        }

        private float antyZugzwangSearch(float currentScore, float alpha, float beta, bool maximize, List<string> moves)
        {
            if (orderMoves)
                moveOrdering.OrderMoves(moves);

            float zugzwangScore = maximize ? float.MinValue : float.MaxValue;
            foreach (var move in moves)
            {
                Column movedColumn = makeMove(move, out Stack<Square> takenSquares, out Square previousSquare, out bool promotion);

                var score = minimax(alpha, beta, 0, !maximize);
                if (maximize)
                {
                    zugzwangScore = Mathf.Max(zugzwangScore, score);
                    alpha = Mathf.Max(alpha, zugzwangScore);
                }
                else
                {
                    zugzwangScore = Mathf.Min(zugzwangScore, score);
                    beta = Mathf.Min(beta, zugzwangScore);
                }

                unmakeMove(movedColumn, takenSquares, previousSquare, promotion);

                if (maximize && score >= currentScore ||
                    !maximize && score <= currentScore)
                {
                    // Found move that leads to better or equal position so it's not zugzwang
                    return score;
                }
            }
            // Every move leads to worse position so we found zugzwang
            return zugzwangScore;
        }

        private bool quiescenceSearch(float alpha, float beta, bool maximize, Player playerToMove, List<string> moves, out float eval)
        {
            eval = 0;

            if (searchAllTakes && playerToMove.CanTake)
                return true;

            if (searchUnsafePositions && !hasAnySafeMove(playerToMove, moves))
                return true;

            eval = EvaluatePosition(playerToMove);

            // Anty zugzwang
            if (antyZugzwang && !_isSearchingZugzwang)
            {
                _isSearchingZugzwang = true;
                eval = antyZugzwangSearch(eval, alpha, beta, maximize, moves);
                _isSearchingZugzwang = false;
            }

            return false;
        }

        private float minimax(float alpha, float beta, int depth, bool maximize)
        {
            // Detect draw by repetition.
            // Returns a draw score even if this position has only appeared once in the game history (for simplicity).
            if (_visitedNonTakePositions.Contains(board.ZobristKey))
            {
                return DRAW;
            }

            Player playerToMove = maximize ? gameManager.ActivePlayer : gameManager.InactivePlayer;
            List<string> moves = playerToMove.GetPossibleMovesAndMultiTakes(true);
            bool canTake = playerToMove.CanTake;

            if (moves.Count == 0)
            {
                return maximize ? INACTIVE_WIN - depth : ACTIVE_WIN + depth;
            }
            else if (moves.Count == 1)
            {
                if (forcedSequencesAsOneMove)
                    depth++;
            }
            else if (depth <= 0)
            {
                if (!quiescenceSearch(alpha, beta, maximize, playerToMove, moves, out float eval))
                    return eval;
            }

            if(orderMoves)
                moveOrdering.OrderMoves(moves);

            if (!canTake)
                _visitedNonTakePositions.Add(board.ZobristKey);

            float score;
            if (maximize)
            {
                score = float.MinValue;
                foreach (var move in moves)
                {
                    Column movedColumn = makeMove(move, out Stack<Square> takenSquares, out Square previousSquare, out bool promotion);

                    score = Mathf.Max(score, minimax(alpha, beta, depth - 1, false));
                    alpha = Mathf.Max(alpha, score);

                    unmakeMove(movedColumn, takenSquares, previousSquare, promotion);

                    if (alpha >= beta)
                        break;
                }
            }
            else
            {
                score = float.MaxValue;
                foreach (var move in moves)
                {
                    Column movedColumn = makeMove(move, out Stack<Square> takenSquares, out Square previousSquare, out bool promotion);

                    score = Mathf.Min(score, minimax(alpha, beta, depth - 1, true));
                    beta = Mathf.Min(beta, score);

                    unmakeMove(movedColumn, takenSquares, previousSquare, promotion);

                    if (beta <= alpha)
                        break;
                }
            }

            if (!canTake)
                _visitedNonTakePositions.Remove(board.ZobristKey);

            return score;
        }

        public string BestMoveMinimax()
        {
            return BestMoveMinimax(searchDepth);
        }

        public string BestMoveMinimax(int depth)
        {
            PiecesManager.FakeMoves = true;
            float bestScore = float.MinValue;
            string bestMove = "";

            List<string> moves = gameManager.ActivePlayer.GetPossibleMovesAndMultiTakes();
            float score;
            if (moves.Count == 1)
            {
                bestMove = moves[0];
            }
            else
            {
                if (orderMoves)
                    moveOrdering.OrderMoves(moves);

                _visitedNonTakePositions.Clear();
                foreach (var p in board.GetPositionsSinceLastTake())
                    _visitedNonTakePositions.Add(p);

                foreach (var move in moves)
                {
                    Column movedColumn = makeMove(move, out Stack<Square> takenSquares, out Square previousSquare, out bool promotion);

                    score = minimax(bestScore, float.MaxValue, depth - 1, false);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                    }

                    unmakeMove(movedColumn, takenSquares, previousSquare, promotion);
                }
            }
            PiecesManager.FakeMoves = false;

            if (moves.Count == 1)
            {
                Debug.Log("forcedMove " + bestMove);
            }
            else
            {
                Debug.Log("bestMove/" + moves.Count + " " + bestMove + " (" + bestScore + ")");
                if(bestScore >= ACTIVE_WIN)
                {
                    Debug.LogError("ACTIVE_WIN found");
                }
                else if (bestScore <= INACTIVE_WIN)
                {
                    Debug.LogError("INACTIVE_WIN found");
                }
            }
            return bestMove;
        }
    }
}