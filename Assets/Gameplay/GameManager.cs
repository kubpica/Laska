using System.Linq;
using UnityEngine;

namespace Laska
{
    public class GameManager : MonoBehaviourSingleton<GameManager>
    {
        public Player[] players;
        public Camera camera;

        private Player activePlayer;

        [GlobalComponent] MoveMaker moveMaker;

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
            activePlayer = player;
            player.RefreshPossibleMoves();

            if (!player.HasPossibleMoves())
            {
                moveMaker.Mate = true;
            }
            else
            {
                moveMaker.SetPlayerToMove(player);
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

        private void nextPlayer()
        {
            char nextColor = activePlayer.color == 'w' ? 'b' : 'w';
            SetActivePlayer(nextColor);
        }

        private void multiTakeDecision()
        {
            setGameState(GameState.Turn);
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
