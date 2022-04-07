using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laska
{
    public class LaskaAI : MonoBehaviourSingleton<LaskaAI>
    {
        [GlobalComponent] private Board board;
        [GlobalComponent] private GameManager gameManager;

        private const int ACTIVE_WIN = 1000000;
        private const int INACTIVE_WIN = -1000000;

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

        private Column makeMove(string move, Stack<string> takenSquares, out Square previousSquare, out bool promotion, Column movedColumn = null)
        {
            //Debug.Log("makeMove " + move + " " + takenSquares.Count);
            var squares = move.Split('-');
            if (movedColumn == null)
            {
                movedColumn = board.GetColumnAt(squares[0]);
                previousSquare = movedColumn.Square;
            }
            else
            {
                previousSquare = null;
            }
            Square targetSquare;
            if (squares.Length == 3)
            {
                // Take
                var takenColumn = board.GetColumnAt(squares[1]);
                targetSquare = board.GetSquareAt(squares[2]);

                takenSquares.Push(takenColumn.Square.coordinate);
                movedColumn.Take(takenColumn);
                promotion = movedColumn.Move(targetSquare);

                movedColumn.CalcPossibleMoves(takenSquares);
                if (movedColumn.PossibleMoves.Count > 0 && !promotion)
                {
                    makeMove(movedColumn.PossibleMoves[0], takenSquares, out _, out promotion, movedColumn);
                    return movedColumn;
                }
            }
            else
            {
                // Move
                targetSquare = board.GetSquareAt(squares[1]);
                previousSquare = movedColumn.Square;
                promotion = movedColumn.Move(targetSquare);
            }

            //if(promotion)
            //Debug.LogError("promotion " + promotion);
            return movedColumn;
        }

        private void unmakeMove(Square lastSquare, Stack<string> takenSquares, Square previousSquare, bool demote)
        {
            var column = lastSquare.Column;

            //Debug.Log("unmake " + column.Square.coordinate + " " + takenSquares.Count + " " + previousSquare.coordinate + " " + demote);
            while (takenSquares.Count > 0)
            {
                var takenSquare = takenSquares.Pop();
                column.Untake(board.GetSquareAt(takenSquare));
            }

            column.Move(previousSquare);

            if (demote)
                column.Demote();
        }

        private int minimax(int alpha, int beta, int depth, bool maximize)
        {
            Player playerToMove = maximize ? gameManager.ActivePlayer : gameManager.InactivePlayer;
            List<string> moves = playerToMove.GetPossibleMoves(true);

            if (moves.Count == 0)
            {
                var r = maximize ? INACTIVE_WIN - depth : ACTIVE_WIN + depth;
                //Debug.LogError("Win found " + r);
                return r;
            }

            if (depth <= 0 && moves.Count > 1)
                return EvaluatePosition(playerToMove);

            int score;
            if (maximize)
            {
                score = int.MinValue;
                foreach (var move in moves)
                {
                    //Debug.Log("maximize make " + move);
                    Stack<string> takenSquares = new Stack<string>();
                    Square lastSquare = makeMove(move, takenSquares, out Square previousSquare, out bool promotion).Square;

                    score = Mathf.Max(score, minimax(alpha, beta, depth - 1, false));
                    alpha = Mathf.Max(alpha, score);

                    //Debug.Log("maximize unmake " + move);
                    unmakeMove(lastSquare, takenSquares, previousSquare, promotion);

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
                    Stack<string> takenSquares = new Stack<string>();
                    Square lastSquare = makeMove(move, takenSquares, out Square previousSquare, out bool promotion).Square;

                    score = Mathf.Min(score, minimax(alpha, beta, depth - 1, true));
                    beta = Mathf.Min(beta, score);

                    //Debug.Log("minimize unmake " + move);
                    unmakeMove(lastSquare, takenSquares, previousSquare, promotion);

                    if (beta <= alpha)
                        break;
                }
            }

            return score;
        }

        public string BestMoveMinimax(int depth)
        {
            PiecesManager.FakeMoves = true;
            int bestScore = int.MinValue;
            string bestMove = "";

            List<string> moves = gameManager.ActivePlayer.GetPossibleMoves();
            int score;
            if (moves.Count == 1)
            {
                bestMove = moves[0];
            }
            else
            {
                foreach (var move in moves)
                {
                    //Debug.Log("first make " + move);
                    Stack<string> takenSquares = new Stack<string>();
                    Square lastSquare = makeMove(move, takenSquares, out Square previousSquare, out bool promotion).Square;

                    score = minimax(bestScore, int.MaxValue, depth - 1, false);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                    }
                    //Debug.Log("first unmake " + move);

                    unmakeMove(lastSquare, takenSquares, previousSquare, promotion);
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