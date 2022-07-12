using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Laska
{
    public class GameManager : MonoBehaviourSingleton<GameManager>
    {
        [GlobalComponent] private MoveMaker moveMaker;
        [GlobalComponent] private Board board;
        [GlobalComponent] private GuiScaler gui;

        static System.Random rnd = new System.Random();

        public Player[] players;

        public UnityEvent onPlayerDecision;
        public UnityEvent onGameStarted;
        public CharEvent onGameEnded;
        public bool markOnlyMovableColumn;
        public bool firstAIMovesRandom;

        /// <summary>
        /// Player to make move.
        /// </summary>
        public Player ActivePlayer { get; private set; }

        /// <summary>
        /// Player waiting for the other player to move.
        /// </summary>
        public Player InactivePlayer => GetPlayer(getOppositeColor(ActivePlayer));

        public Player WhitePlayer => GetPlayer('w');
        public Player BlackPlayer => GetPlayer('b');

        public bool Mate { get; private set; }
        public bool DrawByRepetition { get; private set; }
        public bool DrawByFiftyMoveRule { get; private set; }
        public int HalfMovesCounter { get; private set; }

        public void Reset()
        {
            _gameState = GameState.PreGame;
            Mate = false;
            DrawByRepetition = false;
            DrawByFiftyMoveRule = false;
            HalfMovesCounter = 0;
        }

        public enum GameState
        {
            PreGame,
            Turn,
            TurnResults,
            Ended
        }

        private GameState _gameState;
        public GameState CurrentGameState
        {
            get => _gameState;
            set => _gameState = value;
        }

        public enum AIMode
        {
            PlayerVsPlayer,
            PlayerVsRedAI,
            PlayerVsGreenAI,
            AIVsAI
        }

        private static AIMode _aIMode = AIMode.PlayerVsRedAI;

        public static AIMode GetAIMode() => _aIMode;

        private void Start()
        {
            moveMaker.onMoveStarted.AddListener(moveStarted);
            moveMaker.onMoveEnded.AddListener(moveEnded);
            moveMaker.onMultiTakeDecision.AddListener(multiTakeDecision);

            loadAIMode();
        }

        public void ResetGame()
        {
            if (ActivePlayer != null && ActivePlayer.isAI)
                ActivePlayer.AI.AbortMakeMove();
            SceneManager.LoadScene("Laska");
        }

        public void LoadAIMode(AIMode mode)
        {
            _aIMode = mode;
            ResetGame();
        }

        private void loadAIMode()
        {
            switch (_aIMode)
            {
                case AIMode.PlayerVsPlayer:
                    DisableAI();
                    break;
                case AIMode.PlayerVsRedAI:
                    WhitePlayer.isAI = false;
                    BlackPlayer.isAI = true;
                    if (BlackPlayer == ActivePlayer)
                        makeAIMove();
                    break;
                case AIMode.PlayerVsGreenAI:
                    WhitePlayer.isAI = true;
                    BlackPlayer.isAI = false;
                    if (WhitePlayer == ActivePlayer)
                        makeAIMove();
                    CameraController.Instance.ChangePerspective();
                    break;
                case AIMode.AIVsAI:
                    WhitePlayer.isAI = true;
                    BlackPlayer.isAI = true;
                    if (ActivePlayer != null)
                        makeAIMove();
                    break;
            }
        }

        private void makeAIMove()
        {
            if (firstAIMovesRandom && HalfMovesCounter < 6)
            {
                makeRandomMove();
                return;
            }

            moveMaker.MoveSelectionEnabled = false;
            ActivePlayer.AI.MakeMove();
        }

        private void makeRandomMove()
        {
            var move = GetRandomMove();
            moveMaker.MakeMove(move);
        }

        public string GetRandomMove()
        {
            var moves = ActivePlayer.GetPossibleMovesAndMultiTakes();
            return moves[rnd.Next(moves.Count)];
        }

        public void DisableAI()
        {
            WhitePlayer.isAI = false;
            BlackPlayer.isAI = false;
        }

        public void EnableAI()
        {
            if (_aIMode == AIMode.PlayerVsPlayer)
            {
                _aIMode = InactivePlayer.color == 'w' ? AIMode.PlayerVsGreenAI : AIMode.PlayerVsRedAI;
            }
            loadAIMode();
        }

        public Player GetPlayer(char color)
        {
            return players.First(p => p.color == color);
        }

        public void SetActivePlayer(char color)
        {
            setActivePlayer(players.First(p => p.color == color));
        }

        private void setActivePlayer(Player player)
        {
            player.RefreshPossibleMoves();

            if (!player.HasPossibleMoves())
            {
                Debug.Log("Player " + player.color + " (AI: " + player.isAI + ") has no moves!");
                Mate = true;
                setGameState(GameState.Ended);
                onGameEnded.Invoke(ActivePlayer.color);
                moveMaker.MoveSelectionEnabled = false;
                return;
            }
            else
            {
                gui.SetCurrentTextColor(player);
                moveMaker.SetPlayerToMove(player);
            }
            ActivePlayer = player;

            if (player.isAI)
            {
                // Make AI move
                makeAIMove();
            }
            else
            {
                PiecesManager.TempMoves = false;
                moveMaker.MoveSelectionEnabled = true;
                onPlayerDecision.Invoke();

                if (markOnlyMovableColumn)
                {
                    // Mark the only movable column
                    if(player.CanOnlyOneColumnMove(out Column c))
                    {
                        moveMaker.MarkPossibleMoves(c);
                    }
                }
            }
        }

        private void moveStarted(string move)
        {
            gui.SetLastTextColor(ActivePlayer);
            setGameState(GameState.TurnResults);
        }

        private void moveEnded()
        {
            HalfMovesCounter++;
            if (board.OfficerMovesSinceLastTake >= 100)
            {
                // Fifty-move rule - it's draw when it's 100th half-move without take or Soldier move
                DrawByFiftyMoveRule = true;
                setGameState(GameState.Ended);
                onGameEnded.Invoke('-');
                gui.SetCurrentTextColor(Color.yellow);
            }
            else if (board.IsThreefoldRepetition())
            {
                //TODO check if the position actually repeated 3 times and it's not just hashing collision

                DrawByRepetition = true;
                setGameState(GameState.Ended);
                onGameEnded.Invoke('-');
                gui.SetCurrentTextColor(Color.yellow);
            }
            else
            {
                // Set next player
                setGameState(GameState.Turn);
                nextPlayer();
            }
        }

        private char getOppositeColor(Player p)
        {
            return p.color == 'w' ? 'b' : 'w';
        }

        private void nextPlayer()
        {
            char nextColor = getOppositeColor(ActivePlayer);
            SetActivePlayer(nextColor);
        }

        private void multiTakeDecision()
        {
            setGameState(GameState.Turn);
            moveMaker.MoveSelectionEnabled = true;
        }

        private void setGameState(GameState gs)
        {
            _gameState = gs;
        }

        //private void FixedUpdate()
        //{
        //    if(_gameState == GameState.Turn)
        //    {
        //        activePlayer.timer -= Time.fixedDeltaTime;
        //        if (activePlayer.timer <= 0)
        //        {
        //            activePlayer.timer = 0;
        //            //TODO player lost on time
        //        }
        //    }
        //}
    }
}
