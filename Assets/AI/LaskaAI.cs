using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laska
{
    public class LaskaAI : MonoBehaviourSingleton<LaskaAI>
    {
        [GlobalComponent] private Board board;
        [GlobalComponent] private GameManager gameManager;

        public int searchDepth;
        public bool forcedSequencesAsOneMove;
        public bool searchAllTakes;

        private const int ACTIVE_WIN = 1000000;
        private const int INACTIVE_WIN = -1000000;
        private const int DRAW = -1;

        private HashSet<ulong> _visitedNonTakePositions = new HashSet<ulong>(); // Zobrist keys

        public int EvaluatePosition(Player playerToMove)
        {
            // Check for mate
            int activePlayerPieces = gameManager.ActivePlayer.pieces.Count(p => p.IsFree);
            if (activePlayerPieces == 0)
            {
                //Debug.LogError("INACTIVE_WIN found");
                return INACTIVE_WIN;
            }

            int inactivePlayerPices = gameManager.InactivePlayer.pieces.Count(p => p.IsFree);
            if (inactivePlayerPices == 0)
            {
                //Debug.LogError("ACTIVE_WIN found");
                return ACTIVE_WIN;
            }

            // Check for stalemate (We can skip it as we check it in the minimax func)
            //if (!playerToMove.HasNewPossibleMoves())
            //    return playerToMove == gameManager.ActivePlayer ? INACTIVE_WIN : ACTIVE_WIN;

            int activeScore = 0;
            
            var activePieceDiff = activePlayerPieces - inactivePlayerPices;
            activeScore = activePieceDiff * 10000;

            if (activePieceDiff > 2) //Mathf.Abs(activePieceDiff)
            {
                activeScore += calcDistanceScore();
            }

            return activeScore;
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

        private int minimax(int alpha, int beta, int depth, bool maximize)
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
                var r = maximize ? INACTIVE_WIN - depth : ACTIVE_WIN + depth;
                //Debug.LogError("Win found " + r);
                return r;
            }
            else if (moves.Count == 1)
            {
                if (forcedSequencesAsOneMove)
                    depth++;
            }
            else if (depth <= 0)
            {
                if(!searchAllTakes || !canTake)
                    return EvaluatePosition(playerToMove);
            }

            if (!canTake)
                _visitedNonTakePositions.Add(board.ZobristKey);

            int score;
            if (maximize)
            {
                score = int.MinValue;
                foreach (var move in moves)
                {
                    //Debug.Log("maximize make " + move);
                    Column movedColumn = makeMove(move, out Stack<Square> takenSquares, out Square previousSquare, out bool promotion);

                    score = Mathf.Max(score, minimax(alpha, beta, depth - 1, false));
                    alpha = Mathf.Max(alpha, score);

                    //Debug.Log("maximize unmake " + move);
                    unmakeMove(movedColumn, takenSquares, previousSquare, promotion);

                    if (alpha >= beta)
                        break;
                }
            }
            else
            {
                score = int.MaxValue;
                foreach (var move in moves)
                {
                    //Debug.Log("minimize make " + move);
                    Column movedColumn = makeMove(move, out Stack<Square> takenSquares, out Square previousSquare, out bool promotion);

                    score = Mathf.Min(score, minimax(alpha, beta, depth - 1, true));
                    beta = Mathf.Min(beta, score);

                    //Debug.Log("minimize unmake " + move);
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
            int bestScore = int.MinValue;
            string bestMove = "";

            List<string> moves = gameManager.ActivePlayer.GetPossibleMovesAndMultiTakes();
            int score;
            if (moves.Count == 1)
            {
                bestMove = moves[0];
            }
            else
            {
                _visitedNonTakePositions.Clear();
                foreach (var p in board.GetPositionsSinceLastTake())
                    _visitedNonTakePositions.Add(p);

                foreach (var move in moves)
                {
                    //Debug.Log("first make " + move);
                    Column movedColumn = makeMove(move, out Stack<Square> takenSquares, out Square previousSquare, out bool promotion);

                    score = minimax(bestScore, int.MaxValue, depth - 1, false);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                    }
                    //Debug.Log("first unmake " + move);

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