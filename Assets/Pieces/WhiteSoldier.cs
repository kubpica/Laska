using System.Collections.Generic;

namespace Laska
{
    /// <summary>
    /// "Zielony Żymianin"
    /// </summary>
    public class WhiteSoldier : Piece
    {
        private static readonly List<string> _directions = new List<string> { "++", "-+" };

        public override char Id => 'w';
        public override char PromotionRank => '7';
        public override bool IsOfficer => false;
        public override List<string> MovementDirections => _directions;
        public override int ZobristIndex => 0;

        public override string Mianownik => Theme.Soldier;
        public override string Biernik => Theme.SoldierBiernik;
    }
}
