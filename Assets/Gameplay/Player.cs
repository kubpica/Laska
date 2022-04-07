using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laska
{
    public class Player : MonoBehaviour
    {
        public char color;
        public bool isAI;
        public float timer;
        public List<Piece> pieces = new List<Piece>();
        private IEnumerable<Column> columns;

        /// <summary>
        /// Get columns that have commander of this player.
        /// </summary>
        /// <returns> List of <see cref="Column"/>s controlled by this player.</returns>
        private IEnumerable<Column> getOwnedColums()
        {
            return pieces.Where(p => p.IsFree).Select(p => p.Column).Distinct();
        }

        /// <summary>
        /// Recalculates owned columns and moves this player can make.
        /// </summary>
        /// <remarks>
        /// This should be called before every turn.
        /// </remarks>
        public void RefreshPossibleMoves(List<string> takenSquares = null)
        {
            columns = getOwnedColums();

            foreach(var c in columns)
            {
                if (takenSquares == null)
                    c.CalcPossibleMoves();
                else
                    c.CalcPossibleMoves(takenSquares);
            }

            // If take is possible, remove all non-take moves, as taking is obligatory
            if (columns.Any(c => c.CanTake))
            {
                foreach (var c in columns)
                {
                    if (!c.CanTake)
                        c.PossibleMoves.Clear();
                }
            }
        }

        public bool HasNewPossibleMoves()
        {
            columns = getOwnedColums();
            foreach (var c in columns)
            {
                c.CalcPossibleMoves();
                if (c.PossibleMoves.Count > 0)
                    return true;
            }
            return false;
        }

        public bool HasPossibleMoves()
        {
            foreach(var c in columns)
            {
                if (c.PossibleMoves.Count > 0)
                    return true;
            }
            return false;
        }

        public List<string> GetPossibleMoves(bool refresh = false)
        {
            if(refresh)
                RefreshPossibleMoves();
            List<string> moves = new List<string>();
            foreach (var c in columns)
            {
                moves.AddRange(c.PossibleMoves);
            }
            return moves;
        }
    }
}