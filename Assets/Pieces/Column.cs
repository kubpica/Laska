using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Laska
{
    public class Column : MonoBehaviourExtended
    {
        [GlobalComponent] private GameManager gameManager;
        [GlobalComponent] private PiecesManager piecesManager;
        [GlobalComponent] private Board board;

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
                if (_square != null)
                    _square.Clear();

                // Place the piece on the new square
                _square = value;
                if (_square != null)
                    _square.PlaceColumn(this);
            }
        }
        public Piece Commander => _pieces.First.Value;
        public List<string> PossibleMoves => Commander.PossibleMoves;
        public List<string> MovementDirections => Commander.MovementDirections;
        public bool CanTake => Commander.CanTake;
        public string Position => Commander.Position;

        /// <summary>
        /// How many pieces of the same color are there on the top of this column.
        /// </summary>
        public int Strength
        {
            get
            {
                var p = _pieces.First;
                int i = 1;
                while(p.Next != null && Commander.Color == p.Value.Color)
                {
                    p = p.Next;
                    i++;
                }
                return i;
            }
        }

        #region Value

        private bool _isValueDirtyWhite = true;
        private float _cachedValueWhite;
        private float _cachedSubValueWhite;
        private bool _isValueDirtyBlack = true;
        private float _cachedValueBlack;
        private float _cachedSubValueBlack;

        public float Value
        {
            get
            {
                if(gameManager.ActivePlayer.color == 'w')
                {
                    if (_isValueDirtyWhite)
                    {
                        updateValueWhite();
                    }
                    return _cachedValueWhite;
                }
                else
                {
                    if (_isValueDirtyBlack)
                    {
                        updateValueBlack();
                    }
                    return _cachedValueBlack;
                }
            }
        }

        /// <summary>
        /// The value of the column obtained by removing the current commander.
        /// </summary>
        public float SubValue
        {
            get
            {
                if (gameManager.ActivePlayer.color == 'w')
                {
                    if (_isValueDirtyWhite)
                    {
                        updateValueWhite();
                    }
                    return _cachedSubValueWhite;
                }
                else
                {
                    if (_isValueDirtyBlack)
                    {
                        updateValueBlack();
                    }
                    return _cachedSubValueBlack;
                }
            }
        }

        /// <summary>
        /// Value-SubValue
        /// </summary>
        public float Risk
        {
            get
            {
                if (gameManager.ActivePlayer.color == 'w')
                {
                    if (_isValueDirtyWhite)
                    {
                        updateValueWhite();
                    }
                    return _cachedValueWhite - _cachedSubValueWhite;
                }
                else
                {
                    if (_isValueDirtyBlack)
                    {
                        updateValueBlack();
                    }
                    return _cachedValueBlack - _cachedSubValueBlack;
                }
            }
        }

        private void updateValueWhite()
        {
            _isValueDirtyWhite = false;
            _cachedValueWhite = calcValue(out _cachedSubValueWhite);
        }

        private void updateValueBlack()
        {
            _isValueDirtyBlack = false;
            _cachedValueBlack = calcValue(out _cachedSubValueBlack);
        }

        private float calcValue(out float returnedSubValue)
        {
            var playerAi = gameManager.ActivePlayer.AI;
            var commanderColor = Commander.Color;
            float subValue;
            var returnedValue = visitNode(_pieces.First);
            returnedSubValue = subValue;
            return returnedValue;

            float visitNode(LinkedListNode<Piece> node)
            {
                var piece = node.Value;
                float value = piece.IsOfficer ? playerAi.cfg.officerValue : playerAi.cfg.soldierValue;
                float captivesShare = piece.IsOfficer ? playerAi.cfg.officerCaptivesShare : playerAi.cfg.soldierCaptivesShare;

                // Is it enemy piece?
                int isTeammate = piece.Color == commanderColor ? 1 : -1;
                value *= isTeammate;

                if (node.Next != null)
                {
                    // Calc value of captives
                    subValue = visitNode(node.Next);
                }
                else
                {
                    // No more captives - penalty for losing a column
                    subValue = -1 * isTeammate;
                }
                value += subValue * captivesShare;

                return value;
            }
        }

        #endregion

        private string _description;
        private bool _isDescriptionDirty = true;
        public string Description
        {
            get
            {
                if (_isDescriptionDirty)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"Kolumna na {Position}:");
                    int i = 1;
                    foreach (var p in _pieces)
                    {
                        var color = p.Color == 'b' ? "Czerwony" : "Zielony";
                        sb.AppendLine($"{i}. {color} {p.Mianownik.ToLower()}");
                        i++;
                    }
                    _description = sb.ToString();
                    _isDescriptionDirty = false;
                }
                return _description;
            }
        }

        public void Init(Piece commander)
        {
            if (_pieces != null)
                Debug.LogError("This column was inited already.");

            _pieces = new LinkedList<Piece>();
            _pieces.AddLast(commander);
            commander.transform.parent = transform;
        }

        public void MarkDirty()
        {
            _isValueDirtyWhite = true;
            _isValueDirtyBlack = true;
            _isDescriptionDirty = true;
        }

        private void replaceTopPiece(Piece newPiece)
        {
            MarkDirty();

            var oldPiece = Commander;

            if(!PiecesManager.TempMoves)
                newPiece.transform.position = oldPiece.transform.position;
            newPiece.Column = this;

            var player = piecesManager.GameManager.GetPlayer(oldPiece.Color);
            player.pieces.Remove(oldPiece);
            player.pieces.Add(newPiece);

            _pieces.RemoveFirst();
            piecesManager.Graveyard.KillPiece(oldPiece);
            _pieces.AddFirst(newPiece);
        }

        private void promote()
        {
            var officer = piecesManager.Graveyard.ReviveOfficer(Commander.Color);
            replaceTopPiece(officer);
        }

        public void Demote()
        {
            var soldier = piecesManager.Graveyard.ReviveSoldier(Commander.Color);
            replaceTopPiece(soldier);
        }

        public void ZobristCommander()
        {
            board.ZobristKey ^= Zobrist.piecesArray[Commander.ZobristIndex, Square.draughtsNotationIndex-1, _pieces.Count - 1];
        }

        public void ZobristAll()
        {
            int height = _pieces.Count - 1;
            foreach(var p in _pieces)
            {
                board.ZobristKey ^= Zobrist.piecesArray[p.ZobristIndex, Square.draughtsNotationIndex-1, height];
                height--;
            }
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
            bool promotion;
            if (Position[1] == Commander.PromotionRank)
            {
                if(!ignorePromotion)
                    promote();
                promotion = true;
            }
            else
            {
                promotion = false;
            }

            return promotion;
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
            MarkDirty();

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
            MarkDirty();

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
            MarkDirty();

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
            prevColumn.MarkDirty();
            takenPiece.Column = prevColumn;
        }
    }
}