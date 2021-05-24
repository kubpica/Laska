namespace Laska
{
    public class PositionLoader : MonoBehaviourExtended
    {
        public string laskaFen;

        [GlobalComponent] PiecesSpawner spawner;

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
                            spawner.SpawnColumn(c, file, rank);
                        file += 2;
                    }
                }

                file = 1-(rank % 2);
                rank--;
            }

            GameManager.Instance.SetActivePlayer(playerToMove[0]);
        }
    }

}