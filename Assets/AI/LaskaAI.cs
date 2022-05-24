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
        [Component] private TranspositionTable transpositionTable;

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
        public float pointsPerOwnedColumn = 10000;
        public bool evalColumnsStrength;
        public float pointsPerExtraColumnStrength = 10000;
        public bool evalSpace;
        public bool orderMoves;
        public bool useTranspositionTable;
        public bool useTTForDirectEvals;
        public bool storeBestMoveForAllNodes;
        public bool failSoft;

        public const float ACTIVE_WIN = 1000000;
        public const float INACTIVE_WIN = -1000000;
        /// <summary>
        /// Contempt Factor - https://www.chessprogramming.org/Contempt_Factor
        /// </summary>
        public const float DRAW = -0.5f;

        private HashSet<ulong> _visitedNonTakePositions = new HashSet<ulong>(); // Zobrist keys
        private bool _isSearchingZugzwang;
        private HashSet<string> _cachedSafeSquares = new HashSet<string>();

        public float EvaluatePosition(Player playerToMove)
        {
            var activeColumns = gameManager.ActivePlayer.GetOwnedColums();
            var inactiveColumns = gameManager.InactivePlayer.GetOwnedColums();

            // Check for mate (We can skip it as we check it in the negamax func)
            //int activeColumnsCount = activeColumns.Count();
            //if (activeColumnsCount == 0)
            //{
            //    return INACTIVE_WIN;
            //}

            //int inactiveColumnsCount = inactiveColumns.Count();
            //if (inactiveColumnsCount == 0)
            //{
            //    return ACTIVE_WIN;
            //}

            // Check for stalemate (We can skip it as we check it in the negamax func)
            //if (!playerToMove.HasNewPossibleMoves())
            //    return playerToMove == gameManager.ActivePlayer ? INACTIVE_WIN : ACTIVE_WIN;

            float activeScore = 0;
            
            if(pointsPerOwnedColumn != 0)
            {
                var activePieceDiff = activeColumns.Count() - inactiveColumns.Count();
                activeScore = activePieceDiff * pointsPerOwnedColumn;
            }

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

        private float antyZugzwangSearch(float currentScore, float alpha, float beta, bool maximize,
            List<string> moves, int plyFromRoot, out int repetitions)
        {
            if (orderMoves)
                moveOrdering.OrderMoves(moves, null);

            int evalType = TranspositionTable.UpperBound;
            string bestMove = null;
            float bestScore = float.MinValue;
            repetitions = 0;

            for (int i = 0; i < moves.Count; i++)
            {
                var move = moves[i];
                Column movedColumn = makeMove(move, out Stack<Square> takenSquares, out Square previousSquare, out bool promotion);
                float score = -negamax(-beta, -alpha, 0, !maximize, plyFromRoot + 1, ref repetitions);
                unmakeMove(movedColumn, takenSquares, previousSquare, promotion);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;

                    // Found a new best move so far. (PV-Node?)
                    if (score > alpha)
                    {
                        // Exact score means that it's not influenced by cutoffs,
                        // so even if alpha/beta changed it still would return the same score.
                        evalType = TranspositionTable.Exact;

                        // Move was *too* good (Cut-Node)
                        if (score >= beta)
                        {
                            evalType = TranspositionTable.LowerBound;

                            if (!failSoft)
                                bestScore = beta;

                            break; // beta-cutoff
                        }
                        else
                        {
                            alpha = score;
                        }

                        // Found move that leads to better or equal position so it's not zugzwang.
                        if (score >= currentScore)
                        {
                            // If we haven't checked all the moves, then there might be a better one
                            // but we can still store it as Exact (if it's between [alpha, beta]),
                            // bacause it's good enought to break the zugzwang search.

                            break; // We were just making sure the position is not zugzwang.
                        }
                    }
                }
            }

            // We don't like any move (All-Node)
            if (evalType == TranspositionTable.UpperBound)
            {
                if (!failSoft)
                    bestScore = alpha;

                if (!storeBestMoveForAllNodes)
                    bestMove = null;
            }

            if (useTranspositionTable && repetitions == 0)
                transpositionTable.StoreEvaluation(0, plyFromRoot, bestScore, evalType, bestMove);

            return bestScore;
        }

        /// <returns> Should search be extended?</returns>
        private bool quiescenceSearch(float alpha, float beta, bool maximize,
            Player playerToMove, List<string> moves, int plyFromRoot,
            out float eval, out int repetitions)
        {
            eval = TranspositionTable.LookupFailed;
            repetitions = 0;

            if (searchAllTakes && playerToMove.CanTake)
                return true;

            if (searchUnsafePositions && !hasAnySafeMove(playerToMove, moves))
                return true;

            // Get static evaluation
            if (useTTForDirectEvals)
            {
                eval = transpositionTable.LookupDirectEvaluation(plyFromRoot);
            }
            if (eval == TranspositionTable.LookupFailed)
            {
                eval = applyPerspectiveToEval(EvaluatePosition(playerToMove), maximize);
                // Save static evaluation into transposition table
                if (useTTForDirectEvals)
                {
                    transpositionTable.StoreDirectEvaluation(plyFromRoot, eval);
                }
            }

            // Anty zugzwang
            if (antyZugzwang && !_isSearchingZugzwang)
            {
                _isSearchingZugzwang = true;
                eval = antyZugzwangSearch(eval, alpha, beta, maximize, moves, plyFromRoot, out repetitions);
                _isSearchingZugzwang = false;
            }

            return false;
        }

        private float applyPerspectiveToEval(float eval, bool maximize)
        {
            return maximize ? eval : -eval;
        }

        /// <summary>
        /// Alternative notation of the minimax algorithm in which in every node we maximize,
        /// it's achieved by negating values and applying perspective to eval.
        /// (You can imagine this as if we were turning the eval bar upside down every move.
        /// Alpha would be our lower bound and beta the upper one, and our goal is to find
        /// the best move with a score that fits within that window.)
        /// </summary>
        /// <param name="alpha"> Value of the best move so far of the player to move in this node.</param>
        /// <param name="beta"> Value of the best move of the other player.</param>
        /// <param name="depth"> 
        /// Decreases with every node checked, at depth 0 we run search extensions and then <see cref="EvaluatePosition(Player)"/>.
        /// </param>
        /// <param name="maximize"> 
        /// Whos move it is at this node; true if it's "root player".
        /// (As our eval func returns positive values when the position is good for <see cref="GameManager.ActivePlayer"/>.)
        /// </param>
        /// <param name="plyFromRoot"> Starts with 0 at root node and increases with every node checked.</param>
        /// <param name="repetitionsLastNode"> How many draws by repetition were found starting from this/previous node.</param>
        /// <returns> 
        /// It depends on whether we are using fail-soft or fail-hard version
        /// and whether we fit in the [alpha, beta] window or not,
        /// but the general idea is to return value of the best move in a given position.
        /// </returns>
        private float negamax(float alpha, float beta, int depth, bool maximize, int plyFromRoot, ref int repetitionsLastNode)
        {
            // Detect draw by repetition.
            // Returns a draw score even if this position has only appeared once in the game history (for simplicity).
            if (_visitedNonTakePositions.Contains(board.ZobristKey))
            {
                repetitionsLastNode++;
                return applyPerspectiveToEval(DRAW, maximize);
            }

            // Try looking up the current position in the transposition table.
            // If the same position has already been searched to at least an equal depth
            // to the search we're doing now, we can just use the recorded evaluation.
            string ttMove = null;
            if (useTranspositionTable)
            {
                float ttVal = transpositionTable
                    .LookupEvaluation(_isSearchingZugzwang ? -1 : Mathf.Max(0, depth), plyFromRoot, alpha, beta, out ttMove);
                if (ttVal != TranspositionTable.LookupFailed)
                {
                    return ttVal;
                }
            }

            Player playerToMove = maximize ? gameManager.ActivePlayer : gameManager.InactivePlayer;
            List<string> moves = playerToMove.GetPossibleMovesAndMultiTakes(true);
            bool canTake = playerToMove.CanTake;

            if (moves.Count == 0)
            {
                float eval = maximize ? INACTIVE_WIN + plyFromRoot : ACTIVE_WIN - plyFromRoot;
                return applyPerspectiveToEval(eval, maximize);
            }
            else if (moves.Count == 1)
            {
                if (forcedSequencesAsOneMove)
                    depth++;
            }
            else if (depth <= 0)
            {
                if (!quiescenceSearch(alpha, beta, maximize, playerToMove, moves, plyFromRoot,
                    out float eval, out int repetitions))
                {
                    repetitionsLastNode += repetitions;
                    return eval;
                }
            }

            if(orderMoves)
                moveOrdering.OrderMoves(moves, ttMove);

            if (!canTake)
                _visitedNonTakePositions.Add(board.ZobristKey);

            int evalType = TranspositionTable.UpperBound;
            string bestMove = null;
            float bestScore = float.MinValue;
            int repetitionsThisNode = 0;

            foreach (var move in moves)
            {
                Column movedColumn = makeMove(move, out Stack<Square> takenSquares, out Square previousSquare, out bool promotion);
                float score = -negamax(-beta, -alpha, depth - 1, !maximize, plyFromRoot + 1, ref repetitionsThisNode);
                unmakeMove(movedColumn, takenSquares, previousSquare, promotion);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;

                    // Move was *too* good, so opponent won't allow this position to be reached
                    // (by choosing a different move earlier on, so we "refute" opponents previous move). Skip remaining moves.
                    if (score >= beta)
                    {
                        // Fail-high / Cut-Node
                        // As we stop searching this node here we can't be sure that "bestMove" is actually the best,
                        // but we will still store it in transposition table as it may be good guess for move ordering.
                        // It's called Refutation Move - not necessarily the best, but good enough to refute opponents previous move.
                        // (Also there could be more cutoffs deeper, so it's not guaranteed to be Exact even if it's the last move.)
                        evalType = TranspositionTable.LowerBound;

                        // In Fail-hard version we clamp returned/stored score to beta when we fail-high
                        if (!failSoft)
                            bestScore = beta;

                        break; // beta-cutoff
                    }

                    // Found a new best move so far (if false, we have better move earlier).
                    if (score > alpha)
                    {
                        // Inside-window / PV-Node
                        // It's confirmed to be PV-Node only if it's the last move in this position;
                        // otherwise it still can turn out to be Cut-Node, but it's not All-Node for sure.
                        evalType = TranspositionTable.Exact;

                        alpha = score; // alpha acts like max in MiniMax
                    }
                }
            }

            // We don't like any of the moves in this position.
            // We can do better by choosing a different move earlier on.
            if (evalType == TranspositionTable.UpperBound)
            {
                // Fail-low / All-Node
                // Even though we checked all the moves in this position, most likely there were some cutoffs deeper.
                // The opponent deeper may have even better moves than the ones we checked, so bestScore is our upper bound.

                // bestScore may be lower than alpha here, so in Fail-hard version we clamp it to alpha
                if (!failSoft)
                    bestScore = alpha;

                // Some engines store "bestMove" for All-Nodes, while other don't save any move when all of them failed-low (e.g. Stockfish).
                // I found some old online discussion (https://groups.google.com/g/rec.games.chess.computer/c/p8GbiiLjp0o)
                // where people claim that storing "bestMove" for "All-Nodes" (AlphaFlag) resulted in faster search for them
                // but they are woried that it may not necessery mean stronger play because of increased search instability.
                if (!storeBestMoveForAllNodes)
                    bestMove = null;
            }

            // We shoudn't store in TT scores that were influenced by repetiton draws, because scores stored in TT should only
            // depend on deeper positions and not previous ones (as sometimes we can reach the same position by different path).
            // Draws by repetition can depend on positions prior to this one so we shoudn't store scores based on them in TT.
            //TODO Maybe I should check if the previous move was take, then we could know if repetition was only in deeper nodes.
            //TODO Is it worth to store the "bestMove" anyway? Maybe it would still improve move ordering even if influenced by draws?
            if (useTranspositionTable && repetitionsThisNode == 0)
            {
                transpositionTable
                    .StoreEvaluation(_isSearchingZugzwang ? -1 : Mathf.Max(1, depth), plyFromRoot, bestScore, evalType, bestMove);
            }

            if (!canTake)
                _visitedNonTakePositions.Remove(board.ZobristKey);

            repetitionsLastNode += repetitionsThisNode;

            // In Fail-hard version bestScore is alpha for All-Node, beta for Cut-Node and exact for PV-Node.
            // In Fail-soft version bestScore is not clamped to [alpha, beta] range.
            return bestScore;
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
            if (moves.Count == 1)
            {
                bestMove = moves[0];
            }
            else
            {
                search();
            }
            PiecesManager.FakeMoves = false;

            if (moves.Count == 1)
            {
                Debug.Log("forcedMove " + bestMove);
            }
            else
            {
                Debug.Log("bestMove/" + moves.Count + " " + bestMove + " (" + bestScore + ")");
                if(bestScore >= ACTIVE_WIN-depth)
                {
                    Debug.LogError("ACTIVE_WIN found");
                }
                else if (bestScore <= INACTIVE_WIN+depth)
                {
                    Debug.LogError("INACTIVE_WIN found");
                }
            }
            return bestMove;

            void search()
            {
                string ttMove = null;
                if (useTranspositionTable)
                {
                    float ttVal = transpositionTable
                        .LookupEvaluation(depth, 0, float.MinValue, float.MaxValue, out ttMove);
                    if (ttVal != TranspositionTable.LookupFailed)
                    {
                        bestMove = ttMove;
                        bestScore = ttVal;
                        return;
                    }
                }

                int repetitions = 0;

                if (orderMoves)
                    moveOrdering.OrderMoves(moves, ttMove);

                _visitedNonTakePositions.Clear();
                foreach (var p in board.GetPositionsSinceLastTake())
                    _visitedNonTakePositions.Add(p);

                foreach (var move in moves)
                {
                    Column movedColumn = makeMove(move, out Stack<Square> takenSquares, out Square previousSquare, out bool promotion);

                    float score = -negamax(float.MinValue, -bestScore, depth - 1, false, 1, ref repetitions);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                    }

                    unmakeMove(movedColumn, takenSquares, previousSquare, promotion);
                }

                if (useTranspositionTable && repetitions == 0)
                    transpositionTable.StoreEvaluation(depth, 0, bestScore, TranspositionTable.Exact, bestMove);
            }
        }
    }
}