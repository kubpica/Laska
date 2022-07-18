using UnityEngine;

namespace Laska
{
    public class MenusManager : MonoBehaviourSingleton<MenusManager>
    {
        [Component] public LanguageMenu language;
        [Component] public IngameMenu ingame;
        [Component] public PositionEditorMenu editor;
        [GlobalComponent] public IngameMessages msg;
        [GlobalComponent] private GameManager game;

        public static bool IsEditorActive;

        private void Start()
        {
            if (!LanguageManager.IsLanguageSelected)
            {
                language.gameObject.SetActive(true);
                editor.gameObject.SetActive(false);
                ingame.gameObject.SetActive(false);
                msg.gameObject.SetActive(false);
                return;
            }

            showMenu();
        }

        private void showMenu()
        {
            if (IsEditorActive)
                ShowEditorMenu();
            else
                ShowIngameMenu();
        }

        public void ExitLanguageMenu()
        {
            LanguageManager.IsLanguageSelected = true;
            msg.gameObject.SetActive(true);
            ThemeManager.Instance.ApplyStinkyCheese();
            FenCodec.Instance.Load();
            showMenu();
        }

        public void ShowIngameMenu()
        {
            IsEditorActive = false;
            language.gameObject.SetActive(false);
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
            language.gameObject.SetActive(false);
            editor.gameObject.SetActive(true);
            ingame.gameObject.SetActive(false);
            game.CurrentGameState = GameManager.GameState.Paused;
            Board.Instance.UnmarkAll();
            fixAIMode();
            ingame.SetLevel(IngameMenu.BOT_LEVEL_X);
            msg.DisplayEval = true;
            msg.UpdateEval();
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

