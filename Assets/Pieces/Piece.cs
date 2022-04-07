using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laska
{
    public abstract class Piece : MonoBehaviour
    {
        private static ThemeManager _theme;
        public static ThemeManager Theme
        {
            get
            {
                if (_theme == null)
                    _theme = ThemeManager.Instance;

                return _theme;
            }
        }

        private static Board _board;
        public static Board Board
        {
            get
            {
                if (_board == null)
                    _board = Board.Instance;

                return _board;
            }
        }

        private Column _column;
        public Column Column 
        { 
            get 
            {
                if (_column == null)
                    _column = Column.CreateColumn(this);

                return _column;
            }

            set 
            {
                _column = value;

                if (!PiecesManager.FakeMoves)
                {
                    if (_column != null)
                        transform.parent = _column.transform;
                    else
                        transform.parent = Column.ColumnHolder.transform;
                }
            } 
        }

        public bool HasColumn => _column != null;

        public Square Square => Column.Square;

        /// <summary>
        /// Name of the square at which is the piece.
        /// </summary>
        public string Position => Square.coordinate;

        public bool IsFree => Column.Commander == this;
        public char Color { get; set; }

        private List<string> _possibleMoves;
        public List<string> PossibleMoves 
        {
            get 
            {
                if (_possibleMoves == null)
                    CalcPossibleMoves();

                return _possibleMoves;
            }
        }

        private bool _canTake;
        public bool CanTake
        {
            get
            {
                if (_possibleMoves == null)
                    CalcPossibleMoves();

                return _canTake;
            }
        }

        public abstract char PromotionRank { get; }
        public abstract bool CanGoBackwards { get; }
        public abstract List<string> MovementDirections { get; }

        public abstract string Mianownik { get; }
        public abstract string Biernik { get; }

        private MeshRenderer meshRenderer;
        private Color materialColor;
        private Color materialColorDark;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();

            materialColor = meshRenderer.material.color;

            var dark = new HSBColor(materialColor);
            dark.b = 0.6f;
            materialColorDark = dark.ToColor();
        }

        public void MarkDark()
        {
            meshRenderer.material.color = materialColorDark;
        }

        public void UnmarkDark()
        {
            meshRenderer.material.color = materialColor;
        }

        protected void AddPossibleMove(int fileId, int rankId)
        {
            string move = Position + "-" + Board.GetSquareCoordinate(fileId, rankId);
            _possibleMoves.Add(move);
        }

        protected void AddPossibleMove(int fileId, int rankId, int takesFileId, int takesRankId)
        {
            string move = Position + "-" + Board.GetSquareCoordinate(takesFileId, takesRankId) + "-" + Board.GetSquareCoordinate(fileId, rankId);
            _possibleMoves.Add(move);
        }

        /// <summary>
        /// Used to calculate next possible takes in a multi-take.
        /// </summary>
        /// <param name="takenPieces"> Columns taken so far - these can't be taken again in this multi-take sequence.</param>
        public void CalcPossibleMoves(List<Piece> takenPieces)
        {
            CalcPossibleMoves(takenPieces.Select(p => p.Position));
        }

        public void CalcPossibleMoves(IEnumerable<string> takenSquares)
        {
            CalcPossibleMoves();

            if (_canTake)
            {
                for(int i = _possibleMoves.Count-1; i>=0; i--)
                {
                    var move = _possibleMoves[i];
                    string takenSquare = move.Substring(3, 2);
                    if (takenSquares.Contains(takenSquare))
                    {
                        _possibleMoves.RemoveAt(i);
                    }
                }

                if (_possibleMoves.Count == 0)
                    _canTake = false;
            }
            else if (takenSquares.Count() > 0)
            {
                // No more takes possible, end of the turn
                _possibleMoves.Clear(); // Clear moves, as you can't move after taking
            }
        }

        public void CalcPossibleMoves()
        {
            if (_possibleMoves == null)
                _possibleMoves = new List<string>();
            else
                _possibleMoves.Clear();
            
            Board.GetSquareIds(Position, out int file, out int rank);

            _canTake = false;
            foreach(var dir in MovementDirections)
            {
                int dirX = dir[0] == '-' ? -1 : 1;
                int dirY = dir[1] == '-' ? -1 : 1;
                try
                {
                    var attackedColumn = Board.GetColumnAt(file + 1 * dirX, rank + 1 * dirY);
                    if(attackedColumn != null)
                    {
                        if (Board.GetColumnAt(file + 2 * dirX, rank + 2 * dirY) == null)
                        {
                            // Can take?
                            if (attackedColumn.Commander.Color != Color)
                            {
                                if (!_canTake)
                                {
                                    _possibleMoves.Clear();
                                    _canTake = true;
                                }

                                AddPossibleMove(file + 2 * dirX, rank + 2 * dirY, file + 1 * dirX, rank + 1 * dirY);
                            }
                        }
                    }
                    else if (!_canTake)
                    {
                        // Can move?
                        AddPossibleMove(file + 1*dirX, rank + 1*dirY);
                    }
                }
                catch { }
            }
        }
    }
}