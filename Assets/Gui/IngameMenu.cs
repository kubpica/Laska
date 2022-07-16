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

        private const int BOT_OFF = 9;
        private const int BOT_LEVEL_X = 8;
        private static int s_level = 0;
        private static bool s_rotateAutomatically;
        private static bool s_rotateScreen;

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

        private void setLevel()
        {
            s_level %= 10;
            if (s_level == BOT_OFF)
            {
                levelManager.SetLevel(-1);
            }
            else
            {
                levelManager.SetLevel(s_level);
            }
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

        private void normalGui()
        {
            if (gui.ButtonTopRight(new Rect(340, 10, 305, 80),
                s_level == BOT_OFF ? "Bot wyłączony" : (s_level == BOT_LEVEL_X ? "Poziom X" : "Poziom " + s_level)))
            {
                nextLevel();
            }
            if (game.HalfMovesCounter > (GameManager.GetAIMode() == AIMode.PlayerVsPlayer ? 0 : 1))
            {
                if (gui.ButtonTopRight(new Rect(340, 100, 305, 80), "Nowa gra"))
                {
                    game.ResetGame();
                }
            }
            else
            {
                if (gui.ButtonTopRight(new Rect(340, 100, 305, 80), "Edytuj pozycję"))
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
                    if (gui.ButtonTopRight(new Rect(340, 190, 305, 80), "Auto. obróć: ekran"))
                    {
                        s_rotateScreen = false;
                        s_rotateAutomatically = false;
                    }
                }
                else
                {
                    if (gui.ButtonTopRight(new Rect(340, 190, 305, 80), "Auto. obróć: planszę"))
                    {
                        s_rotateScreen = true;
                    }
                }
            }
            else
            {
                if (gui.ButtonTopRight(new Rect(340, 190, 100, 80), "Obróć"))
                {
                    cameraController.ChangePerspective();
                }

                if (gui.ButtonTopRight(new Rect(230, 190, 195, 80), "automatycznie"))
                {
                    s_rotateAutomatically = true;
                }
            }

            if (game.HalfMovesCounter < 2)
            {
                modeSelection();
            }
        }

        private void modeSelection()
        {
            if (modeButton(0, "Graj zielonymi", AIMode.PlayerVsRedAI))
            {
                s_rotateAutomatically = false;
                fixLevel();
                game.LoadAIMode(AIMode.PlayerVsRedAI);
            }

            if (modeButton(1, "Graj czerwonymi", AIMode.PlayerVsGreenAI))
            {
                s_rotateAutomatically = false;
                fixLevel();
                game.LoadAIMode(AIMode.PlayerVsGreenAI);
            }

            if (modeButton(2, "Gracz vs gracz", AIMode.PlayerVsPlayer))
            {
                s_rotateAutomatically = true;
                s_level = BOT_OFF;
                game.LoadAIMode(AIMode.PlayerVsPlayer);
            }

            if (modeButton(3, "Bot vs bot", AIMode.AIVsAI))
            {
                s_rotateAutomatically = false;
                s_level = BOT_LEVEL_X;
                game.LoadAIMode(AIMode.AIVsAI);
            }

            bool modeButton(int i, string text, AIMode mode)
            {
                return gui.ButtonTopRight(new Rect(255, 320 + 90 * i, 220, 80), text, GameManager.GetAIMode() == mode);
            }
        }

        private void upsideDownGui()
        {
            if (gui.ButtonBottomRight(new Rect(255, 90, 220, 80), "Obróć ekran"))
            {
                cameraController.UpsideDown = false;
            }
        }
    }
}
