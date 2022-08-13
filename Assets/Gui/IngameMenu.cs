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
        [GlobalComponent] private InappReview review;

        private static bool s_rotateAutomatically;
        private static bool s_rotateScreen;

        private bool _isReviewBlocked = false;
        private bool _isReviewLoading = false;

        private int Level
        {
            get => levelManager.CurrentLevel;
            set => levelManager.SetLevel(value);
        }

        private Language Language => LanguageManager.Language;

        private void Start()
        {
            MoveMaker.Instance.onMoveEnded.AddListener(moveEnded);

            game.onGameEnded.AddListener(_ => review.RequestReview());
            review.onRequestFailed.AddListener(() => _isReviewBlocked = true);
            review.onLaunchFailed.AddListener(() => 
            {
                _isReviewBlocked = true;
                menus.msg.DisplayedMsg = Language.reviewFailed + "\n" + review.Error;
                openAppPage();
            });
            review.onLaunchFinished.AddListener(() => 
            {
                _isReviewLoading = false;
                _isReviewBlocked = true;
            });
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
            Level = (Level + 1)%10;
        }

        private void fixLevel()
        {
            if (Level == LevelManager.BOT_OFF
                || Level == LevelManager.BOT_1SEC_LEVEL && GameManager.GetAIMode() == AIMode.AIVsAI)
            {
                Level = 0;
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

        private string currentlevelName() => 
            Level == LevelManager.BOT_1SEC_LEVEL 
            ? Language.level + $" ? (1 {Language.secondAbbreviated})" 
            : Language.level + " " + Level;

        private void normalGui()
        {
            if (gui.ButtonTopRight(new Rect(340, 10, 305, 80),
                Level == LevelManager.BOT_OFF ? Language.botOff : currentlevelName()))
            {
                nextLevel();
            }
            if (game.HalfMovesCounter > (GameManager.GetAIMode() == AIMode.PlayerVsPlayer ? 0 : 1))
            {
                if (gui.ButtonTopRight(new Rect(340, 100, 305, 80), Language.newGame))
                {
                    if (GameManager.GetAIMode() == AIMode.AIVsAI)
                    {
                        game.LoadAIMode(AIMode.PlayerVsRedAI);
                    }
                    else
                    {
                        game.ResetGame();
                    }
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

            if (game.CurrentGameState == GameState.Ended)
            {
                gameEndedMenu();
            }
            else if (game.IsAIThinking && game.ActivePlayer.AI.SearchStopwatch.ElapsedMilliseconds > 3000)
            {
                if (gui.ButtonTopRight(new Rect(255, 300, 220, 80), Language.forceMove))
                {
                    game.ActivePlayer.AI.EndSearch();
                }
            }
            else if (game.HalfMovesCounter < 2)
            {
                modeSelection();
                gui.SoundButton(680);
            }
            else
            {
                gui.SoundButton(300);
            }
        }

        private void openAppPage()
        {
            Application.OpenURL("https://play.google.com/store/apps/details?id=com.NiebieskiPunkt.Checkers3D");
        }

        private void gameEndedMenu()
        {
            int i = 0;
            if (!game.ActivePlayer.isAI && !_isReviewBlocked)
            {
                if (_isReviewLoading)
                {
                    if (button(Language.loading))
                    {
                        openAppPage();
                    }
                }
                else if (button(Language.reviewApp))
                {
#if UNITY_ANDROID
                    review.LaunchReview();
                    _isReviewLoading = true;
#else
                    openAppPage();
#endif
                }
                i++;
            }

            if (button(Language.myOtherGames))
            {
                if (Language.myOtherGames.StartsWith("My"))
                {
#if UNITY_ANDROID
                    Application.OpenURL("https://play.google.com/store/apps/details?id=com.NiebieskiPunkt.HeadBumper");
#else
                    Application.OpenURL("https://store.steampowered.com/app/1398130/Head_Bumper_Editcraft");
#endif
                }
                else
                {
                    Application.OpenURL("https://play.google.com/store/apps/developer?id=Niebieski+Punkt");
                }
            }

            gui.SoundButton(405 + 90 * i);

            bool button(string text)
            {
                return gui.ButtonTopRight(new Rect(255, 300 + 90 * i, 220, 80), text);
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
                Level = LevelManager.BOT_OFF;
                game.LoadAIMode(AIMode.PlayerVsPlayer);
            }

            if (modeButton(3, Language.botVsBot, AIMode.AIVsAI, gui.LightGray, Color.yellow))
            {
                s_rotateAutomatically = false;
                Level = LevelManager.BOT_1SEC_LEVEL;
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
