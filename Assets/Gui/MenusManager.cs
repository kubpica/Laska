using UnityEngine;

namespace Laska
{
    public class MenusManager : MonoBehaviourSingleton<MenusManager>
    {
        [Component] public IngameMenu ingame;
        [Component] public PositionEditorMenu editor;
        [GlobalComponent] public IngameMessages msg;
        [GlobalComponent] private GameManager game;

        public static bool IsEditorActive;

        private void Start()
        {
            if (IsEditorActive)
                ShowEditorMenu();
            else
                ShowIngameMenu();
        }

        public void ShowIngameMenu()
        {
            IsEditorActive = false;
            editor.gameObject.SetActive(false);
            ingame.gameObject.SetActive(true);
            if(game.CurrentGameState == GameManager.GameState.Paused)
                game.CurrentGameState = GameManager.GameState.Turn;
            fixAIMode();
            msg.DisplayEval = false;
        }

        public void ShowEditorMenu()
        {
            IsEditorActive = true;
            editor.gameObject.SetActive(true);
            ingame.gameObject.SetActive(false);
            game.CurrentGameState = GameManager.GameState.Paused;
            Board.Instance.UnmarkAll();
            fixAIMode();
            msg.DisplayEval = true;
            ingame.SetLevel(IngameMenu.BOT_LEVEL_X);
        }

        private void fixAIMode()
        {
            if (game.HalfMovesCounter % 2 == 0)
                return;

            var mode = GameManager.GetAIMode();
            if (mode == GameManager.AIMode.PlayerVsGreenAI || mode == GameManager.AIMode.PlayerVsRedAI)
            {
                mode = game.ActivePlayer.color == 'w' ? GameManager.AIMode.PlayerVsGreenAI : GameManager.AIMode.PlayerVsRedAI;
                game.PresetAIMode(mode);
            }
        }
    }
}

