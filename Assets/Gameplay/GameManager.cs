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

        [GlobalComponent] MoveMaker moveMaker;
        [GlobalComponent] LaskaAI ai;

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
            setActivePlayer(players[0]);

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
            ActivePlayer = player;
            player.RefreshPossibleMoves();

            if (!player.HasPossibleMoves())
            {
                Debug.Log("Player " + player.color + " (AI: " + player.isAI + ") has no moves!");
                moveMaker.Mate = true;
                return;
            }
            else
            {
                moveMaker.SetPlayerToMove(player);
            }

            if (player.isAI)
            {
                moveMaker.MoveSelectionEnabled = false;
                // Make AI move
                var move = ai.BestMoveMinimax();
                moveMaker.MakeMove(move);
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
            // Set next player
            nextPlayer();

            setGameState(GameState.Turn);
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
