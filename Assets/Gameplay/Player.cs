using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laska
{
    public class Player : MonoBehaviour
    {
        public char color;
        public float timer;
        public List<Piece> pieces = new List<Piece>();
        private IEnumerable<Column> columns;

        /// <summary>
        /// Get columns that have commander of this player.
        /// </summary>
        /// <returns> List of <see cref="Column"/>s controlled by this player.</returns>
        private IEnumerable<Column> getOwnedColums()
        {
            return pieces.Where(p => p.IsFree).Select(p => p.Column);
        }

        /// <summary>
        /// Recalculates owned columns and moves this player can make.
        /// </summary>
        /// <remarks>
        /// This should be called before every turn.
        /// </remarks>
        public void RefreshPossibleMoves()
        {
            columns = getOwnedColums();

            foreach(var c in columns)
            {
                c.CalcPossibleMoves();
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

        public bool HasPossibleMoves()
        {
            foreach(var c in columns)
            {
                if (c.PossibleMoves.Count > 0)
                    return true;
            }
            return false;
        }
    }
}