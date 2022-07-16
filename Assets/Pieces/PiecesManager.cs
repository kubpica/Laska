using System.Linq;
using UnityEngine;

namespace Laska
{
    public class PiecesManager : MonoBehaviourSingleton<PiecesManager>
    {
        /// <summary>
        /// Set to true when AI analyzes moves (without actually making them) so some optimizations can be applied.
        /// </summary>
        public static bool TempMoves;

        [Component] private Graveyard _graveyard;
        [Component] private PiecesSpawner _spawner;
        [GlobalComponent] private GameManager _gameManager;
        [GlobalComponent] private Board _board;

        public GameObject ColumnHolder => gameObject;
        public Graveyard Graveyard => _graveyard;
        public PiecesSpawner Spawner => _spawner;
        public GameManager GameManager => _gameManager;

        public bool IsPiecesLimitReached { get; private set; }

        public void RemovePiece(Square square)
        {
            RemovePiece(square.Column);
        }

        public void RemovePiece(Column column)
        {
            if (column == null)
                return;

            var pieces = column.Pieces;
            var toRemove = pieces.First.Value;

            pieces.RemoveFirst();
            _board.UnregisterPiece(toRemove);
            Destroy(toRemove.gameObject);
            IsPiecesLimitReached = false;

            if(pieces.Count == 0)
                Destroy(column.gameObject);
        }

        public void RemoveColumn(Square square)
        {
            RemoveColumn(square.Column);
        }

        public void RemoveColumn(Column column)
        {
            if (column == null)
                return;

            while (column.Pieces.Count > 0)
                RemovePiece(column);
        }

        public void AddPiece(char pieceId, Square square)
        {
            var column = square.Column;
            if (column == null)
            {
                AddColumn(pieceId.ToString(), square);
            }
            else
            {
                AddPiece(pieceId, column);
            }
        }

        public void AddPiece(char pieceId, Column column)
        {
            if (isPiecesLimitReached())
            {
                Debug.LogError("The limit of 100 pieces has been reached!");
                return;
            }

            var piece = _spawner.SpawnPiece(pieceId);
            var p = column.Commander.transform.position;
            p.y += piece.transform.localScale.y / 2.0f;
            piece.transform.position = p;

            _board.RegisterPiece(piece);
            column.AddOnTop(piece);
        }

        private bool isPiecesLimitReached()
        {
            IsPiecesLimitReached = 100 <= _gameManager.WhitePlayer.pieces.Count + _gameManager.BlackPlayer.pieces.Count;
            return IsPiecesLimitReached;
        }

        public Column AddColumn(string c, int fileId, int rankId)
        {
            var square = _board.GetSquareAt(fileId, rankId);
            return AddColumn(c, square);
        }

        public Column AddColumn(string c, Square square)
        {
            if (isPiecesLimitReached())
            {
                Debug.LogError("The limit of 100 pieces has been reached!");
                return null;
            }

            var column = _spawner.SpawnColumn(c);

            Vector3 p = square.transform.position;
            p.y = 0;

            column.transform.position = p;
            foreach (var piece in column.Pieces.Reverse())
            {
                piece.transform.position = p;
                p.y += piece.transform.localScale.y / 2.0f;
            }

            _board.RegisterColumn(column, square);

            return column;
        }

        /// <summary>
        /// Spawns officers and puts them in <see cref="Graveyard"/> to be waiting there for promotion.
        /// </summary>
        public void PrepareOfficers()
        {
            foreach (var p in GetComponentsInChildren<Piece>())
            {
                if (p is Officer == false)
                {
                    var officer = Spawner.SpawnPiece(char.ToUpperInvariant(p.Color)) as Officer;
                    Graveyard.KillOfficer(officer);
                }
            }
        }

        public void Clear()
        {
            var holder = ColumnHolder.transform;
            for (int i = 0; i<holder.childCount; i++)
            {
                Destroy(holder.GetChild(i).gameObject);
            }

            Graveyard.Clear();
        }
    }
}