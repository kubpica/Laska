using UnityEngine;

namespace Laska
{
    public class PiecesSpawner : MonoBehaviourExtended
    {
        public GameObject whiteSoldier;
        public GameObject whiteOfficer;
        public GameObject blackSoldier;
        public GameObject blackOfficer;

        public Column SpawnColumn(string c)
        {
            var commander = SpawnPiece(c[0]);

            var column = commander.Column;
            if (c.Length > 1)
            {
                for (int i = 1; i < c.Length; i++)
                {
                    var p = SpawnPiece(c[i]);
                    column.Take(p);
                }
            }

            return column;
        }

        public Piece SpawnPiece(char p)
        {
            Piece piece;
            switch (p)
            {
                case 'w':
                    piece = Instantiate(whiteSoldier).GetComponent<Piece>();
                    piece.transform.eulerAngles = new Vector3(0, 180, 0);
                    break;
                case 'W':
                    piece = Instantiate(whiteOfficer).GetComponent<Piece>();
                    piece.transform.eulerAngles = new Vector3(0, 180, 0);
                    break;
                case 'b':
                    piece = Instantiate(blackSoldier).GetComponent<Piece>();
                    break;
                case 'B':
                    piece = Instantiate(blackOfficer).GetComponent<Piece>();
                    break;
                default:
                    Debug.LogError("Piece '" + p + "' not found.");
                    return null;
            }

            piece.transform.parent = transform;
            piece.Color = char.ToLowerInvariant(p);

            return piece;
        }
    }
}