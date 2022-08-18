using UnityEngine;

namespace Laska
{
    public class ExitApp : MonoBehaviourExtended
    {
        [GlobalComponent] private GameManager game;

        private static bool s_readyToExit;
        public bool ReadyToExit { get; set; }

        private void Start()
        {
            if (s_readyToExit)
            {
                s_readyToExit = false;
                ReadyToExit = true;
            }
            else
            {
                ReadyToExit = false;
            }

            game.onPlayerDecision.AddListener(() => ReadyToExit = false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (game.ActivePlayer != null && game.HalfMovesCounter < 2)
                {
                    s_readyToExit = true;
                    LanguageManager.IsLanguageSelected = false;
                    game.ResetGame();
                    return;
                }

                if (ReadyToExit)
                {
                    Application.Quit();
                }
                else
                {
                    ReadyToExit = true;
                    IngameMessages.Instance.DisplayedMsg += LanguageManager.Language.areYouSureExit;
                }
            }
        }
    }
}