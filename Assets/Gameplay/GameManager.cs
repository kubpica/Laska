using System.Linq;
using UnityEngine;

namespace Laska
{
    public class GameManager : MonoBehaviourSingleton<GameManager>
    {
        public Player[] players;

        /// <summary>
        /// Player to make move.
        /// </summary>
        public Player ActivePlayer { get; private set; }

        /// <summary>
        /// Player waiting for the other player to move.
        /// </summary>
        public Player InactivePlayer => GetPlayer(getOppositeColor(ActivePlayer));

        public bool Mate { get; set; }
        public bool DrawByRepetition { get; set; }
        public bool DrawByFiftyMoveRule { get; set; }

        [GlobalComponent] MoveMaker moveMaker;
        [GlobalComponent] Board board;

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

        private void Start()
        {
            moveMaker.onMoveStarted.AddListener(moveStarted);
            moveMaker.onMoveEnded.AddListener(moveEnded);
            moveMaker.onMultiTakeDecision.AddListener(multiTakeDecision);
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
                return;
            }
            else
            {
                moveMaker.SetPlayerToMove(player);
            }
            ActivePlayer = player;

            if (player.isAI)
            {                
                // Make AI move
                moveMaker.MoveSelectionEnabled = false;
                player.AI.MakeMove();
            }
            else
            {
                moveMaker.MoveSelectionEnabled = true;
            }
        }

        private void moveStarted(string move)
        {
            setGameState(GameState.TurnResults);
        }

        private void moveEnded()
        {
            if (board.OfficerMovesSinceLastTake >= 100)
            {
                // Fifty-move rule - it's draw when it's 100th half-move without take or Soldier move
                setGameState(GameState.Ended);
                DrawByFiftyMoveRule = true;
                IngameMessages.Instance.SetCurrentTextColor(Color.yellow);
            }
            else if (board.IsThreefoldRepetition())
            {
                //TODO check if the position actually repeated 3 times and it's not just hashing collision

                setGameState(GameState.Ended);
                DrawByRepetition = true;
                IngameMessages.Instance.SetCurrentTextColor(Color.yellow);
            }
            else
            {
                // Set next player
                nextPlayer();
                setGameState(GameState.Turn);
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
            /*if (ActivePlayer.isAI)
            {
                // Make AI move
                moveMaker.MakeMove(moveMaker.selectedColumn.PossibleMoves[0]);
            }*/
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
        //            //TODO gracz przegrywa na czas
        //        }
        //    }
        //}
    }
}
