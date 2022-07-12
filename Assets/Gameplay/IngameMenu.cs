using System.Collections.Generic;
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

        private const int BOT_OFF = 9;
        private const int BOT_LEVEL_X = 8;
        private static int _level = 0;
        private static bool rotateAutomatically;
        private static bool rotateScreen;

        private void Start()
        {
            MoveMaker.Instance.onMoveEnded.AddListener(moveEnded);
            setLevel();
        }

        private void moveEnded()
        {
            if (rotateAutomatically)
                rotate();
            if (rotateScreen)
                cameraController.UpsideDown = !cameraController.UpsideDown;
        }

        private void rotate()
        {
            cameraController.ChangePerspective();

            checkColumns(game.ActivePlayer.Columns);
            checkColumns(game.InactivePlayer.Columns);

            void checkColumns(IEnumerable<Column> columns)
            {
                if (columns == null)
                    return;

                foreach (var c in columns)
                {
                    cameraController.MakeSureObjectCanBeSeen(c.Commander.gameObject);
                }
            }
        }

        private void nextLevel()
        {
            if (_level == BOT_OFF)
                game.EnableAI();

            _level++;
            setLevel();
        }

        private void setLevel()
        {
            _level %= 10;
            if (_level == BOT_OFF)
            {
                levelManager.SetLevel(-1);
            }
            else
            {
                levelManager.SetLevel(_level);
            }
        }

        private void fixLevel()
        {
            if (_level == BOT_OFF
                || _level == BOT_LEVEL_X && GameManager.GetAIMode() == AIMode.AIVsAI)
            {
                _level = 0;
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
                _level == BOT_OFF ? "Bot wyłączony" : (_level == BOT_LEVEL_X ? "Poziom X" : "Poziom " + _level)))
            {
                nextLevel();
            }

            if (gui.ButtonTopRight(new Rect(340, 100, 305, 80), "Nowa gra"))
            {
                game.ResetGame();
            }

            if (rotateAutomatically)
            {
                if (rotateScreen)
                {
                    if (gui.ButtonTopRight(new Rect(340, 190, 305, 80), "Auto. obróć: ekran"))
                    {
                        rotateScreen = false;
                        rotateAutomatically = false;
                    }
                }
                else
                {
                    if (gui.ButtonTopRight(new Rect(340, 190, 305, 80), "Auto. obróć: planszę"))
                    {
                        rotateScreen = true;
                    }
                }
            }
            else
            {
                if (gui.ButtonTopRight(new Rect(340, 190, 100, 80), "Obróć"))
                {
                    rotate();
                }

                if (gui.ButtonTopRight(new Rect(230, 190, 195, 80), "automatycznie"))
                {
                    rotateAutomatically = true;
                }
            }

            if (game.HalfMovesCounter < 2)
            {
                if (gui.ButtonTopRight(new Rect(255, 320, 220, 80), "Graj zielonymi"))
                {
                    rotateAutomatically = false;
                    fixLevel();
                    game.LoadAIMode(AIMode.PlayerVsRedAI);
                }

                if (gui.ButtonTopRight(new Rect(255, 320 + 90 * 1, 220, 80), "Graj czerwonymi"))
                {
                    rotateAutomatically = false;
                    fixLevel();
                    game.LoadAIMode(AIMode.PlayerVsGreenAI);
                }

                if (gui.ButtonTopRight(new Rect(255, 320 + 90 * 2, 220, 80), "Gracz vs gracz"))
                {
                    rotateAutomatically = true;
                    _level = BOT_OFF;
                    game.LoadAIMode(AIMode.PlayerVsPlayer);
                }

                if (gui.ButtonTopRight(new Rect(255, 320 + 90 * 3, 220, 80), "Bot vs bot"))
                {
                    rotateAutomatically = false;
                    _level = BOT_LEVEL_X;
                    game.LoadAIMode(AIMode.AIVsAI);
                }
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
