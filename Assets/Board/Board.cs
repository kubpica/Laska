using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laska
{
    public class Board : MonoBehaviourSingleton<Board>
    {
        private Square[,] squares = new Square[7,7];
        private HashSet<MeshRenderer> marked = new HashSet<MeshRenderer>();

        [GlobalComponent] GameManager gameManager;

        private void Start()
        {
            // Init squares
            foreach(var s in GetComponentsInChildren<Square>())
            {
                GetSquareIds(s.coordinate, out int file, out int rank);
                squares[file, rank] = s;
            }

            // Make sure every square is inited
            foreach(var s in squares)
            {
                if(s == null)
                {
                    Debug.LogError("Not every square is inited!");
                }
            }
        }

        public string GetSquareCoordinate(int fileId, int rankId)
        {
            char file = (char)('a' + fileId);
            rankId++;
            return file + "" + rankId;
        }

        public int CalcDistance(string coord1, string coord2)
        {
            GetSquareIds(coord1, out int fileId1, out int rankId1);
            GetSquareIds(coord2, out int fileId2, out int rankId2);
            return Mathf.Max(Mathf.Abs(fileId1-fileId2), Mathf.Abs(rankId1 - rankId2));
        }

        public void GetSquareIds(string coordinate, out int fileId, out int rankId)
        {
            fileId = coordinate[0] - 'a';
            rankId = Helpers.stoi(coordinate[1].ToString()) - 1;
        }

        public Square GetSquareAt(string coordinate)
        {
            GetSquareIds(coordinate, out int file, out int rank);
            return GetSquareAt(file, rank);
        }

        public Square GetSquareAt(int fileId, int rankId)
        {
            if (fileId < 0 || fileId > 6 || rankId < 0 || rankId > 6)
                throw new ArgumentOutOfRangeException("file/rank", "Specified unknown square. (the board is 7x7)");

            return squares[fileId, rankId];
        }

        public Column GetColumnAt(string coordinate) => GetSquareAt(coordinate).Column;

        /// <summary>
        /// Get column (of pieces) at specified square.
        /// </summary>
        /// <param name="fileId"> Start from 0. 'a' file = 0</param>
        /// <param name="rankId"> 1st rank = 0</param>
        /// <returns> A column or null.</returns>
        public Column GetColumnAt(int fileId, int rankId) => GetSquareAt(fileId, rankId).Column;

        public void MarkSquare(string square, Color color)
        {
            char file = square[0];
            int rank = Helpers.stoi(square[1].ToString());

            var mr = transform.Find(file.ToString()).GetChild(rank).GetComponent<MeshRenderer>();
            mr.material.color = color;
            marked.Add(mr);
        }

        public void UnmarkAll()
        {
            foreach(var mr in marked)
            {
                mr.material.color = Color.white;
            }

            marked.Clear();
        }

        /// <summary>
        /// Adds column to the game - registers it.
        /// </summary>
        /// <remarks>
        /// Every piece in the column should be newly created as the pieces will be added to the player's list of pieces.
        /// </remarks>
        /// <param name="column"> Column to register.</param>
        public void AddColumn(Column column, Square square)
        {
            // Place on the square
            column.Square = square;
            column.gameObject.name = "Column " + square.coordinate;

            // Add to the player's list of pieces
            var white = gameManager.players.First(p => p.color == 'w');
            var black = gameManager.players.First(p => p.color == 'b');
            foreach (var p in column.Pieces)
            {
                var player = p.Color == 'w' ? white : black;
                player.pieces.Add(p);
            }
        }
    }
}
