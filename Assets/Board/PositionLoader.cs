namespace Laska
{
    public class PositionLoader : MonoBehaviourExtended
    {
        public string laskaFen;

        [GlobalComponent] private PiecesManager piecesManager;
        [GlobalComponent] private GameManager gameManager;

        void Start()
        {
            load(laskaFen);
        }

        private void load(string fen)
        {
            int file = 0;
            int rank = 6;

            string playerToMove = fen.Substring(fen.LastIndexOf(" ") + 1, 1);
            fen = fen.Substring(0, fen.LastIndexOf(" "));

            var rankDsc = fen.Split('/');
            foreach(var r in rankDsc)
            {
                if (!string.IsNullOrEmpty(r))
                {
                    var columnDsc = r.Split(',');
                    foreach (var c in columnDsc)
                    {
                        if(!string.IsNullOrEmpty(c))
                            piecesManager.Spawner.SpawnColumn(c, file, rank);
                        file += 2;
                    }
                }

                file = 1-(rank % 2);
                rank--;
            }

            prepareOfficers();

            char colorToMove = playerToMove[0];
            Board.Instance.ZobristKey = Zobrist.CalcZobristKey(colorToMove);
            gameManager.SetActivePlayer(colorToMove);
            gameManager.onGameStarted.Invoke();

        }

        /// <summary>
        /// Spawns officers and puts them in <see cref="Graveyard"/> to be waiting there for promotion.
        /// </summary>
        private void prepareOfficers()
        {
            foreach (var p in piecesManager.GetComponentsInChildren<Piece>())
            {
                if (p is Officer == false)
                {
                    var officer = piecesManager.Spawner.SpawnPiece(char.ToUpperInvariant(p.Color)) as Officer;
                    piecesManager.Graveyard.KillOfficer(officer);
                }
            }
        }
    }

}