using System.Text;

namespace Laska
{
    public class FenCodec : MonoBehaviourSingleton<FenCodec>
    {
        public static string SavedFen;
        public string initialFen;

        [GlobalComponent] private PiecesManager piecesManager;
        [GlobalComponent] private GameManager gameManager;
        [GlobalComponent] private Board board;
        [GlobalComponent] private CameraController cameraController;

        public const string DEFAULT_POSITION = "b,b,b,b/b,b,b/b,b,b,b/,,/w,w,w,w/w,w,w/w,w,w,w w";

        void Start()
        {
            if (LanguageManager.IsLanguageSelected)
                Load();
        }

        public void Load()
        {
            if (!string.IsNullOrEmpty(SavedFen))
            {
                try
                {
                    load(SavedFen);
                    if (SavedFen != DEFAULT_POSITION)
                    {
                        gameManager.firstAIMovesRandom = false;
                    }
                }
                catch
                {
                    RestoreDefaultPostion();
                }
                return;
            }


            if (!string.IsNullOrEmpty(initialFen))
                load(initialFen);
            else
                load(DEFAULT_POSITION);
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
                            piecesManager.AddColumn(c, file, rank);
                        file += 2;
                    }
                }

                file = 1-(rank % 2);
                rank--;
            }

            piecesManager.PrepareOfficers();

            char colorToMove = playerToMove[0];
            board.ZobristKey = Zobrist.CalcZobristKey(colorToMove);
            gameManager.SetActivePlayer(colorToMove);
            gameManager.InactivePlayer.RefreshOwnedColumns();
            gameManager.onGameStarted.Invoke();
            
            if (colorToMove == 'b')
            {
                var mode = GameManager.GetAIMode();
                if (mode == GameManager.AIMode.AIVsAI || mode == GameManager.AIMode.PlayerVsPlayer)
                {
                    cameraController.ChangePerspective();
                }
            }
        }

        private string getCurrentFen()
        {
            var sb = new StringBuilder();
            for(int rank = 6; rank>=0; rank--)
            {
                for (int file = 0; file<7; file++)
                {
                    var s = board.GetSquareAt(file, rank);
                    if (s.draughtsNotationIndex == 0)
                        continue;

                    appendColumn(s.Column);
                    if (file < 5)
                        sb.Append(',');
                }

                if (rank > 0)
                    sb.Append('/');
            }
            sb.Append(" ").Append(gameManager.ActivePlayer.color);
            return sb.ToString();

            void appendColumn(Column column)
            {
                if (column == null)
                    return;

                foreach (var p in column.Pieces)
                {
                    sb.Append(p.Id);
                }
            }
        }

        private void save(string fen)
        {
            SavedFen = fen;
        }

        public void SaveCurrentPosition()
        {
            save(getCurrentFen());
        }

        public void Reload()
        {
            gameManager.ResetGame();
        }

        public void RestoreDefaultPostion()
        {
            save(DEFAULT_POSITION);
            Reload();
        }
    }

}