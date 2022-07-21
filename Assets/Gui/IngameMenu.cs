using UnityEngine;
using static Laska.GameManager;

namespace Laska
{
    public class IngameMenu : MonoBehaviourExtended
    {
        [GlobalComponent] private GameManager game;
        [GlobalComponent] private LevelManager levelManager;
        [GlobalComponent] private CameraController cameraController;
        [GlobalComponent] private GuiScaler gui;
        [GlobalComponent] private MenusManager menus;
        [GlobalComponent] private ThemeManager theme;

        public const int BOT_OFF = 9;
        public const int BOT_LEVEL_X = 8;
        public static int s_level = 0;
        private static bool s_rotateAutomatically;
        private static bool s_rotateScreen;

        private Language Language => LanguageManager.Language;

        private void Start()
        {
            MoveMaker.Instance.onMoveEnded.AddListener(moveEnded);
            setLevel();
        }

        private void moveEnded()
        {
            if (s_rotateAutomatically)
                cameraController.ChangePerspective();
            if (s_rotateScreen)
                cameraController.UpsideDown = !cameraController.UpsideDown;
        }

        private void nextLevel()
        {
            if (s_level == BOT_OFF)
                game.EnableAI();

            s_level++;
            setLevel();
        }

        public void SetLevel(int level)
        {
            if (level == BOT_OFF)
            {
                levelManager.SetLevel(-1);
            }
            else
            {
                levelManager.SetLevel(level);
            }
        }

        private void setLevel()
        {
            s_level %= 10;
            SetLevel(s_level);
        }

        private void fixLevel()
        {
            if (s_level == BOT_OFF
                || s_level == BOT_LEVEL_X && GameManager.GetAIMode() == AIMode.AIVsAI)
            {
                s_level = 0;
            }
        }

        private void OnGUI()
        {
            if (cameraController.UpsideDown)
            {
                upsideDownGui();
            }
            else
            {
                normalGui();
            }   
        }

        private string currentlevelName() => s_level == BOT_LEVEL_X ? Language.level + " X" : Language.level + " " + s_level;

        private void normalGui()
        {
            if (gui.ButtonTopRight(new Rect(340, 10, 305, 80),
                s_level == BOT_OFF ? Language.botOff : currentlevelName()))
            {
                nextLevel();
            }
            if (game.HalfMovesCounter > (GameManager.GetAIMode() == AIMode.PlayerVsPlayer ? 0 : 1))
            {
                if (gui.ButtonTopRight(new Rect(340, 100, 305, 80), Language.newGame))
                {
                    game.ResetGame();
                }
            }
            else
            {
                if (gui.ButtonTopRight(new Rect(340, 100, 305, 80), theme.EditPosition))
                {
                    if (game.HalfMovesCounter > 0 || game.CurrentGameState == GameState.TurnResults || PiecesManager.TempMoves)
                    {
                        menus.ShowEditorMenu();
                        game.ResetGame();
                    }
                    else
                    {
                        menus.ShowEditorMenu();
                    }
                }
            }
                

            if (s_rotateAutomatically)
            {
                if (s_rotateScreen)
                {
                    if (gui.ButtonTopRight(new Rect(340, 190, 305, 80), $"{Language.autoRotate} {Language.screen}"))
                    {
                        s_rotateScreen = false;
                        s_rotateAutomatically = false;
                    }
                }
                else
                {
                    if (gui.ButtonTopRight(new Rect(340, 190, 305, 80), $"{Language.autoRotate} {Language.board}"))
                    {
                        s_rotateScreen = true;
                    }
                }
            }
            else
            {
                if (gui.ButtonTopRight(new Rect(340, 190, 100, 80), Language.rotate))
                {
                    cameraController.ChangePerspective();
                }

                if (gui.ButtonTopRight(new Rect(230, 190, 195, 80), Language.automatically))
                {
                    s_rotateAutomatically = true;
                }
            }

            if (game.HalfMovesCounter < 2)
            {
                modeSelection();
                gui.SoundButton(680);
            }
            else
            {
                gui.SoundButton(300);
            }
        }

        private void modeSelection()
        {
            if (modeButton(0, Language.playAsGreen, AIMode.PlayerVsRedAI, Color.green, Color.green))
            {
                s_rotateAutomatically = false;
                fixLevel();
                game.LoadAIMode(AIMode.PlayerVsRedAI);
            }

            if (modeButton(1, Language.playAsRed, AIMode.PlayerVsGreenAI, Color.red, Color.red))
            {
                s_rotateAutomatically = false;
                fixLevel();
                game.LoadAIMode(AIMode.PlayerVsGreenAI);
            }

            if (modeButton(2, Language.playerVsPlayer, AIMode.PlayerVsPlayer, gui.LightGray, Color.yellow))
            {
                s_rotateAutomatically = true;
                s_level = BOT_OFF;
                game.LoadAIMode(AIMode.PlayerVsPlayer);
            }

            if (modeButton(3, Language.botVsBot, AIMode.AIVsAI, gui.LightGray, Color.yellow))
            {
                s_rotateAutomatically = false;
                s_level = BOT_LEVEL_X;
                game.LoadAIMode(AIMode.AIVsAI);
            }

            bool modeButton(int i, string text, AIMode mode, Color deselectedColor, Color selectedColor)
            {
                var miniColor = Color.Lerp(gui.ButtonStyle.normal.textColor, deselectedColor, 0.1f);
                return gui.ButtonTopRight(new Rect(255, 300 + 90 * i, 220, 80), text, GameManager.GetAIMode() == mode, miniColor, selectedColor);
            }
        }

        private void upsideDownGui()
        {
            if (gui.ButtonBottomRight(new Rect(255, 90, 220, 80), $"{Language.rotate} {Language.screen}"))
            {
                cameraController.UpsideDown = false;
            }
        }
    }
}
