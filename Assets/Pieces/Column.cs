using System.Collections.Generic;
using UnityEngine;

namespace Laska
{
    public class Column : MonoBehaviour
    {
        public static Column CreateColumn(Piece commander)
        {
            var go = new GameObject("Column");
            go.transform.position = commander.transform.position; 
            go.transform.parent = commander.transform.parent;
            commander.transform.parent = go.transform;
            
            var column = go.AddComponent<Column>();
            column.Init(commander);

            return column;
        }

        private Queue<Piece> _pieces;
        public Queue<Piece> Pieces => _pieces;

        private Square _square;
        public Square Square 
        {
            get => _square;
            
            set
            {
                if (_square == value)
                    return;

                // Empty the old square
                if(_square != null)
                    _square.Clear();

                // Place the piece on the new square
                _square = value;
                if(_square != null)
                    _square.PlaceColumn(this);
            }
        }
        public Piece Commander => _pieces.Peek();
        public List<string> PossibleMoves => Commander.PossibleMoves;
        public bool CanTake => Commander.CanTake;
        public string Position => Commander.Position;

        public void Init(Piece commander)
        {
            if (_pieces != null)
                Debug.LogError("This column was inited already.");

            _pieces = new Queue<Piece>();
            _pieces.Enqueue(commander);
        }

        public void Promote()
        {
            var commander = Commander;
            if (commander is Officer)
                return;

            var officer = PiecesSpawner.Instance.SpawnPiece(char.ToUpperInvariant(commander.Color));
            commander.enabled = false;
            officer.transform.position = commander.transform.position;
            officer.transform.parent = transform;
            officer.Column = this;

            var player = GameManager.Instance.GetPlayer(commander.Color);
            player.pieces.Remove(commander);
            player.pieces.Add(officer);

            _pieces.Dequeue();
            Destroy(commander.gameObject);

            if(_pieces.Count == 0)
            {
                _pieces.Enqueue(officer);
                return;
            }

            var newQueue = new Queue<Piece>();
            newQueue.Enqueue(officer);
            foreach (var p in _pieces)
                newQueue.Enqueue(p);

            _pieces.Clear();
            _pieces = newQueue;
        }

        /// <summary>
        /// <see cref="Piece.CalcPossibleMoves()"/> of <see cref="Commander"/>.
        /// </summary>
        public void CalcPossibleMoves()
        {
            Commander.CalcPossibleMoves();
        }

        /// <summary>
        /// <see cref="Piece.CalcPossibleMoves(List{Column})"/> of <see cref="Commander"/>.
        /// </summary>
        public void CalcPossibleMoves(List<Piece> takenPieces)
        {
            Commander.CalcPossibleMoves(takenPieces);
        }

        /// <summary>
        /// Takes commander of the specified column as prisoner of this column.
        /// </summary>
        /// <param name="column"> Column of which commander to take.</param>
        public void Take(Column column)
        {
            var piece = column.Release();
            Take(piece);
        }

        public void Take(Piece piece)
        {
            if (piece.HasColumn)
            {
                Take(piece.Column);
                return;
            }

            piece.IsFree = false;
            _pieces.Enqueue(piece);

            piece.transform.parent = transform;
            piece.Column = this;
        }

        /// <summary>
        /// Removes current commander so that the next piece on the stack becomes one.
        /// </summary>
        /// <returns> Released commander - a piece on the top.</returns>
        public Piece Release()
        {
            var ex = _pieces.Dequeue();
            ex.transform.parent = transform.parent;
            ex.Column = null;

            if(_pieces.Count > 0)
            {
                Commander.IsFree = true;
            }
            else
            {
                _square.Clear();
                Destroy(this.gameObject);
            }

            return ex;
        }
    }
}