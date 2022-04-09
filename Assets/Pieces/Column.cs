using System.Collections.Generic;
using UnityEngine;

namespace Laska
{
    public class Column : MonoBehaviourExtended
    {
        [GlobalComponent] private PiecesManager piecesManager;

        private static GameObject _columnHolder;
        public static GameObject ColumnHolder
        {
            get
            {
                if (_columnHolder == null)
                    _columnHolder = PiecesManager.Instance.ColumnHolder;

                return _columnHolder;
            }
        }

        public static Column CreateColumn(Piece commander)
        {
            var go = new GameObject("new Column");

            go.transform.position = commander.transform.position.Y(0);

            go.transform.parent = ColumnHolder.transform;
            commander.transform.parent = go.transform;
            
            var column = go.AddComponent<Column>();
            column.Init(commander);

            return column;
        }

        private LinkedList<Piece> _pieces;
        public LinkedList<Piece> Pieces => _pieces;

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
        public Piece Commander => _pieces.First.Value;
        public List<string> PossibleMoves => Commander.PossibleMoves;
        public bool CanTake => Commander.CanTake;
        public string Position => Commander.Position;

        public void Init(Piece commander)
        {
            if (_pieces != null)
                Debug.LogError("This column was inited already.");

            _pieces = new LinkedList<Piece>();
            _pieces.AddLast(commander);
            commander.transform.parent = transform;
        }

        private void replaceTopPiece(Piece newPiece)
        {
            var oldPiece = Commander;

            if(!PiecesManager.FakeMoves)
                newPiece.transform.position = oldPiece.transform.position;
            newPiece.Column = this;

            var player = piecesManager.GameManager.GetPlayer(oldPiece.Color);
            player.pieces.Remove(oldPiece);
            player.pieces.Add(newPiece);

            _pieces.RemoveFirst();
            piecesManager.Graveyard.KillPiece(oldPiece);
            _pieces.AddFirst(newPiece);
        }

        public void Promote()
        {
            var officer = piecesManager.Graveyard.ReviveOfficer(Commander.Color);
            replaceTopPiece(officer);
        }

        public void Demote()
        {
            var soldier = piecesManager.Graveyard.ReviveSoldier(Commander.Color);
            replaceTopPiece(soldier);
        }

        /// <summary>
        /// Moves the column to the indicated square, and if it reached the <see cref="Piece.PromotionRank"/>, the top piece is promoted.
        /// </summary>
        /// <param name="targetSquare"> Destination.</param>
        /// <returns> True if the piece has been promoted.</returns>
        public bool Move(Square targetSquare, bool ignorePromotion = false)
        {
            Square = targetSquare;

            // Check for promotion
            if (Position[1] == Commander.PromotionRank)
            {
                if(!ignorePromotion)
                    Promote();
                return true;
            }
            return false;
        }

        #region Calc possible moves

        /// <summary>
        /// <see cref="Piece.CalcPossibleMoves()"/> of <see cref="Commander"/>.
        /// </summary>
        public void CalcPossibleMoves()
        {
            Commander.CalcPossibleMoves();
        }

        /// <summary>
        /// <see cref="Piece.CalcPossibleMoves(List{Piece})"/> of <see cref="Commander"/>.
        /// </summary>
        public void CalcPossibleMoves(List<Piece> takenPieces)
        {
            Commander.CalcPossibleMoves(takenPieces);
        }

        /// <summary>
        /// <see cref="Piece.CalcPossibleMoves(IEnumerable{string})"/> of <see cref="Commander"/>.
        /// </summary>
        public void CalcPossibleMoves(IEnumerable<string> takenSquares)
        {
            Commander.CalcPossibleMoves(takenSquares);
        }

        /// <summary>
        /// <see cref="Piece.CalcPossibleMovesNewList()"/> of <see cref="Commander"/>.
        /// </summary>
        public List<string> CalcPossibleMovesNewList()
        {
            return Commander.CalcPossibleMovesNewList();
        }

        /// <summary>
        /// <see cref="Piece.CalcPossibleMovesNewList(IEnumerable{string})"/> of <see cref="Commander"/>.
        /// </summary>
        public List<string> CalcPossibleMovesNewList(IEnumerable<string> takenSquares)
        {
            return Commander.CalcPossibleMovesNewList(takenSquares);
        }

        #endregion

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

            _pieces.AddLast(piece);

            piece.Column = this;
        }

        /// <summary>
        /// Removes current commander so that the next piece on the stack becomes one.
        /// </summary>
        /// <returns> Released commander - a piece on the top.</returns>
        public Piece Release()
        {
            var ex = _pieces.First.Value;
            _pieces.RemoveFirst();

            ex.Column = null;

            if(_pieces.Count <= 0)
            {
                _square.Clear();
                piecesManager.Graveyard.KillColumn(this);
            }

            return ex;
        }

        public void Untake(Square takenFrom)
        {
            // Relase the bottom piece
            var takenPiece = _pieces.Last.Value;
            _pieces.RemoveLast();

            // Return the piece to its previous position
            Column prevColumn;
            if (takenFrom.IsEmpty)
            {
                prevColumn = piecesManager.Graveyard.ReviveColumn();
                prevColumn.Square = takenFrom;
            }
            else
            {
                prevColumn = takenFrom.Column;
            }
            prevColumn.Pieces.AddFirst(takenPiece);
            takenPiece.Column = prevColumn;
        }
    }
}