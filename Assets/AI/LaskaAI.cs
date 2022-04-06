using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laska
{
    public class LaskaAI : MonoBehaviourSingleton<LaskaAI>
    {
        [GlobalComponent] private Board board;
        [GlobalComponent] private GameManager gameManager;

        public int EvaluatePosition(Player playerToMove)
        {
            var whitePieceDiff = (gameManager.ActivePlayer.pieces.Count(p => p.IsFree) - gameManager.InactivePlayer.pieces.Count(p => p.IsFree));

            if (Mathf.Abs(whitePieceDiff) > 2)
                whitePieceDiff += board.DistanceBetweenPieces(); //TODO

            return whitePieceDiff * (playerToMove == gameManager.ActivePlayer ? 1 : -1);
        }

        private Column makeMove(string move, List<string> takenSquares, out Square previousSquare, out bool promotion, Column movedColumn = null)
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

                takenSquares.Add(takenColumn.Square.coordinate);
                movedColumn.Take(takenColumn);
                movedColumn.CalcPossibleMoves(takenSquares);
                promotion = movedColumn.Move(targetSquare);

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

        private void unmakeMove(Square lastSquare, List<string> takenSquares, Square previousSquare, bool demote)
        {
            var column = lastSquare.Column;

            //Debug.Log("unmake " + column.Square.coordinate + " " + takenSquares.Count + " " + previousSquare.coordinate + " " + demote);
            while (takenSquares.Count > 0)
            {
                var takenSquare = takenSquares[takenSquares.Count - 1];
                takenSquares.RemoveAt(takenSquares.Count - 1);

                column.Untake(board.GetSquareAt(takenSquare));
            }

            column.Move(previousSquare);

            if (demote)
                column.Demote();
        }

        private int minimax(int alpha, int beta, int depth, bool maximize)
        {
            int i, score;
            List<string> moves;
            Player playerToMove = maximize ? gameManager.ActivePlayer : gameManager.InactivePlayer;
            //Debug.Log("depth " + depth);
            if (depth == 0)
                return EvaluatePosition(playerToMove);

            moves = playerToMove.GetPossibleMoves(true);
            //Debug.Log("moves.Count " + moves.Count);

            if (moves.Count == 0)
                return maximize ? -1000000 : 1000000; //return EvaluatePosition(playerToMove);

            if (maximize)
            {
                score = int.MinValue;
                foreach (var move in moves)
                {
                    //Debug.Log("maximize make " + move);
                    List<string> takenSquares = new List<string>();
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
                    List<string> takenSquares = new List<string>();
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
                    List<string> takenSquares = new List<string>();
                    Square lastSquare = makeMove(move, takenSquares, out Square previousSquare, out bool promotion).Square;

                    score = minimax(int.MinValue, int.MaxValue, depth - 1, false);
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

            Debug.Log("bestMove " + bestMove);
            return bestMove;
        }
    }
}