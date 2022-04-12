using System.Collections.Generic;

namespace Laska
{
    /// <summary>
    /// "Czerwony Żymianin"
    /// </summary>
    public class BlackSoldier : Piece
    {
        private static readonly List<string> _directions = new List<string> { "+-", "--" };

        public override char PromotionRank => '1';
        public override bool CanGoBackwards => false;
        public override List<string> MovementDirections => _directions;
        public override int ZobristIndex => 2;

        public override string Mianownik => Theme.SoldierMianownik;
        public override string Biernik => Theme.SoldierBiernik;

    }
}
