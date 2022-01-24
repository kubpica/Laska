﻿using System.Collections.Generic;

namespace Laska
{
    /// <summary>
    /// "Zielony Żymianin"
    /// </summary>
    public class WhiteSoldier : Piece
    {
        private static readonly List<string> _directions = new List<string> { "++", "-+" };
        
        public override char PromotionRank => '7';
        public override bool CanGoBackwards => false;
        public override List<string> MovementDirections => _directions;

        public override string Mianownik => Theme.SoldierMianownik;
        public override string Biernik => Theme.SoldierBiernik;
    }
}
