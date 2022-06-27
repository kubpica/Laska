using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Laska
{
    public class Player : MonoBehaviourExtended
    {
        [GlobalComponent] private Board board;

        public char color;
        public bool isAI;
        public float timer;
        public List<Piece> pieces = new List<Piece>();
        
        private IEnumerable<Column> _columns;
        public IEnumerable<Column> Columns => _columns;

        private bool _canTake;
        public bool CanTake => _canTake;

        private LaskaAI _ai;
        public LaskaAI AI 
        { 
            get 
            { 
                if(_ai == null)
                {
                    _ai = GetComponent<LaskaAI>();
                    if (_ai == null)
                        _ai = gameObject.AddComponent<LaskaAI>();
                }
                return _ai;
            } 
        }

        /// <summary>
        /// Get columns that have commander of this player.
        /// </summary>
        /// <returns> List of <see cref="Column"/>s controlled by this player.</returns>
        public IEnumerable<Column> GetOwnedColums()
        {
            return pieces.Where(p => p.IsFree).Select(p => p.Column);
        }

        public string GetName()
        {
            return color == 'b' ? "Black" : "White";
        }

        /// <summary>
        /// Recalculates owned columns and moves this player can make.
        /// </summary>
        /// <remarks>
        /// This should be called before every turn.
        /// </remarks>
        public void RefreshPossibleMoves(List<string> takenSquares = null)
        {
            _columns = GetOwnedColums();

            foreach(var c in _columns)
            {
                if (takenSquares == null)
                    c.CalcPossibleMoves();
                else
                    c.CalcPossibleMoves(takenSquares);
            }

            // If take is possible, remove all non-take moves, as taking is obligatory
            _canTake = _columns.Any(c => c.CanTake);
            if (_canTake)
            {
                foreach (var c in _columns)
                {
                    if (!c.CanTake)
                        c.PossibleMoves.Clear();
                }
            }
        }

        /// <summary>
        /// Recalculates possible moves of owned columns until find one with possible moves...
        /// </summary>
        /// <remarks>
        /// It will not update <see cref="Column.PossibleMoves"/>.
        /// To make sure the cached moves are updated use <see cref="RefreshPossibleMoves(List{string})"/>.
        /// </remarks>
        /// <returns>...then returns true, false if no legal moves possible.</returns>
        public bool HasNewPossibleMoves()
        {
            var columns = GetOwnedColums();
            foreach (var c in columns)
            {
                if (c.CalcPossibleMovesNewList().Count > 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check if there is any possible move cached.
        /// </summary>
        /// <returns> False if no legal moves possible.</returns>
        public bool HasPossibleMoves()
        {
            foreach(var c in _columns)
            {
                if (c.PossibleMoves.Count > 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Puts all possible moves and takes (not checking for multi-takes) in one list.
        /// </summary>
        /// <param name="refresh"> If true recalculates possible moves, should be true when new position happens.</param>
        /// <returns> List of moves written in chess style, e.g. c3-d4. (A take is written by mentioning all three squares)</returns>
        public List<string> GetPossibleMoves(bool refresh = false)
        {
            if(refresh)
                RefreshPossibleMoves();
            List<string> moves = new List<string>();
            foreach (var c in _columns)
            {
                moves.AddRange(c.PossibleMoves);
            }
            return moves;
        }

        /// <summary>
        /// Same as <see cref="GetPossibleMoves(bool)"/> but checks for multi-takes and saves every possible path as separate move.
        /// </summary>
        /// <returns> List of moves - multi-takes will mention all squares of its path eq. d2-c3-b4-c5-d6.</returns>
        public List<string> GetPossibleMovesAndMultiTakes(bool refresh = false)
        {
            if (refresh)
                RefreshPossibleMoves();
            var moves = _columns.SelectMany(c => c.PossibleMoves);

            // Check if returned moves or takes, if moves we can skip the multi-take check
            if (!_canTake)
                return moves.ToList(); // All possible moves (no takes)

            var multiMoves = new List<string>();
            var takenSquares = new Stack<string>();
            StringBuilder sb = new StringBuilder();
            // Check for multi-takes
            foreach(var move in moves)
            {
                visitMultiTake(sb, multiMoves, takenSquares, move);
                sb.Clear();
            }
            return multiMoves; // All possible take paths
        }

        private void visitMultiTake(StringBuilder sb, List<string> movesList, Stack<string> takenSquares, string nextMove,
            Column movedColumn = null)
        {
            // Parse next move
            var squares = nextMove.Split('-');
            if(movedColumn == null)
            {
                movedColumn = board.GetColumnAt(squares[0]);
                sb.Append(squares[0]);
            }
            Square previousSquare = movedColumn.Square;
            string takenSquare = squares[1];
            Square targetSquare = board.GetSquareAt(squares[2]);

            // Make next move
            takenSquares.Push(takenSquare);
            bool promotion = movedColumn.Move(targetSquare, true);

            // Save multi-take move notation
            sb.Append("-").Append(takenSquare).Append("-").Append(targetSquare.coordinate);

            // Check for next takes
            bool noMoreTakes = true;
            if (!promotion)
            {
                var possibleMoves = movedColumn.CalcPossibleMovesNewList(takenSquares);
                if (possibleMoves.Count > 0)
                {
                    noMoreTakes = false;
                    for (int i = 0; i < possibleMoves.Count; i++)
                    {
                        visitMultiTake(sb, movesList, takenSquares, possibleMoves[i], movedColumn);
                    }
                }
            }

            // If no more takes add the multi-take move to the list
            if (noMoreTakes)
            {
                movesList.Add(sb.ToString());
            }

            // Undo move
            takenSquares.Pop();
            movedColumn.Move(previousSquare);
            sb.Remove(sb.Length - 6, 6);
        }
    }
}