using System.Collections.Generic;

namespace Laska
{
    /// <summary>
    /// "Zbycholud"
    /// </summary>
    public class Officer : Piece
    {
        private static readonly List<string> _directions = new List<string> { "++", "-+", "+-", "--" };

        public override char Id => char.ToUpper(Color);
        public override char PromotionRank => '-';
        public override bool IsOfficer => true;
        public override List<string> MovementDirections => _directions;
        public override int ZobristIndex => Color == 'w' ? 1 : 3;

        public override string Mianownik => Theme.OfficerMianownik;
        public override string Biernik => Theme.OfficerBiernik;
    }
}
