using System;
using UnityEngine;

namespace Laska
{
    public class Square : MonoBehaviour
    {
        /// <summary>
        /// Name of the square - its coordinates/notation eg. d4.
        /// </summary>
        public string coordinate;

        /// <summary>
        /// Id of the square in draughts notation (1-25, 0 = unused).
        /// </summary>
        public int draughtsNotationIndex;

        private Column _column;
        /// <summary>
        /// Column standing on this square, null if empty.
        /// </summary>
        public Column Column => _column;

        public bool IsEmpty => Column == null;

        /// <summary>
        /// This method should only be called by the <see cref="Column.Square"/> setter! Use it instead.
        /// </summary>
        public void PlaceColumn(Column column)
        {
            if(column.Square != this)
            {
                throw new Exception("This method should only be called by the Column.Square setter!");
            }

            _column = column;
        }

        /// <summary>
        /// Makes the square empty.
        /// </summary>
        public void Clear()
        {
            _column = null;
        }
    }
}
