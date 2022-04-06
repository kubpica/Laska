﻿using System.Collections.Generic;
using UnityEngine;

namespace Laska
{
    public class Graveyard : MonoBehaviour
    {
        private Stack<Column> _columns = new Stack<Column>();
        private Stack<Officer> _whileOfficers = new Stack<Officer>();
        private Stack<Officer> _blackOfficers = new Stack<Officer>();
        private Stack<WhiteSoldier> _whileSoldiers = new Stack<WhiteSoldier>();
        private Stack<BlackSoldier> _blackSoldiers = new Stack<BlackSoldier>();

        public void KillColumn(Column column)
        {
            if(column.Square != null)
            {
                column.Square.Clear();
                column.Square = null;
            }
            column.gameObject.SetActive(false);
            _columns.Push(column);
        }

        public Column ReviveColumn()
        {
            var c = _columns.Pop();
            c.gameObject.SetActive(true);
            return c;
        }

        public void KillPiece(Piece piece)
        {
            piece.gameObject.SetActive(false);
            if(piece is Officer officer)
            {
                killOfficer(officer);
            }
            else if (piece is WhiteSoldier whiteSoldier)
            {
                _whileSoldiers.Push(whiteSoldier);
            }
            else if (piece is BlackSoldier blackSoldier)
            {
                _blackSoldiers.Push(blackSoldier);
            }
        }

        public void KillOfficer(Officer officer)
        {
            officer.gameObject.SetActive(false);
            killOfficer(officer);
        }

        private void killOfficer(Officer officer)
        {
            if (officer.Color == 'w')
                _whileOfficers.Push(officer);
            else
                _blackOfficers.Push(officer);
        }

        public Piece ReviveSoldier(char color)
        {
            Piece p;
            if (color == 'w')
                p = _whileSoldiers.Pop();
            else
                p = _blackSoldiers.Pop();

            p.gameObject.SetActive(true);
            return p;
        }

        public Officer ReviveOfficer(char color)
        {
            Officer o;
            if (color == 'w')
                o = _whileOfficers.Pop();
            else
                o = _blackOfficers.Pop();

            o.gameObject.SetActive(true);
            return o;
        }
    }
}