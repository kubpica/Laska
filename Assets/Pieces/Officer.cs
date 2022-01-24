using System.Collections.Generic;

namespace Laska
{
    /// <summary>
    /// "Zbycholud"
    /// </summary>
    public class Officer : Piece
    {
        private static readonly List<string> _directions = new List<string> { "++", "-+", "+-", "--" };

        public override char PromotionRank => '-';
        public override bool CanGoBackwards => true;
        public override List<string> MovementDirections => _directions;

        public override string Mianownik => Theme.OfficerMianownik;
        public override string Biernik => Theme.OfficerBiernik;
    }
}
