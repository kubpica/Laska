using UnityEngine;
using static Laska.PositionEditor;

namespace Laska
{
    public class PositionEditorMenu : MonoBehaviourExtended
    {
        [GlobalComponent] private GuiScaler gui;
        [GlobalComponent] private MenusManager menus;
        [GlobalComponent] private GameManager game;
        [GlobalComponent] private CameraController cameraController;
        [GlobalComponent] private PositionEditor editor;
        [GlobalComponent] private FenCodec fen;
        [GlobalComponent] private IngameMessages msg;
        [GlobalComponent] private PiecesManager pieces;

        private void OnEnable()
        {
            msg.DisplayedMsg = "Edytor: Usuwanie";
            gui.SetLastTextColor(gui.LightGray);
        }

        private void OnDisable()
        {
            msg.DisplayedMsg = "";
        }

        private void OnGUI()
        {
            if (gui.ButtonTopRight(new Rect(340, 10, 305, 80), "Opuść edytor"))
            {
                // Save postion & exit editor
                fen.SaveCurrentPosition();
                menus.ShowIngameMenu();
                fen.Reload();
            }

            if (gui.ButtonTopRight(new Rect(340, 100, 305, 80), "Przywróć domyślną"))
            {
                // Restore default postion
                fen.RestoreDefaultPostion();
            }

            if (gui.ButtonTopRight(new Rect(340, 190, 305, 80), "Ruch " + (game.ActivePlayer.color == 'b' ? "czerwonego" : "zielonego"),
                game.ActivePlayer.GetActualColor()))
            {
                // Change player to move
                game.WhitePlayer.RefreshOwnedColumns();
                game.BlackPlayer.RefreshOwnedColumns();
                cameraController.ChangePerspective();
                game.NextPlayer();
                msg.UpdateEval();
                gui.SetCurrentTextColor(game.ActivePlayer.GetActualColor());
            }

            toolSelection();
        }

        private void toolSelection()
        {
            toolButton(0, "Usuwanie", PositionEditorTool.Delete, gui.LightGray);
            if (pieces.IsPiecesLimitReached)
                return;

            toolButton(1, "Zielony szeregowy", PositionEditorTool.GreenSoldier, Color.green);
            toolButton(2, "Zielony oficer", PositionEditorTool.GreenOfficer, Color.green);
            toolButton(3, "Czerwony szeregowy", PositionEditorTool.RedSoldier, Color.red);
            toolButton(4, "Czerwony oficer", PositionEditorTool.RedOfficer, Color.red);

            void toolButton(int i, string text, PositionEditorTool tool, Color color)
            {
                var deselectedColor = Color.Lerp(gui.ButtonStyle.normal.textColor, color, 0.15f);
                if (gui.ButtonTopRight(new Rect(315, 300 + 90 * i, 280, 80), text, editor.SelectedTool == tool, deselectedColor))
                {
                    if (CheckPiecesLimit())
                    {
                        editor.SelectedTool = PositionEditorTool.Delete;
                        return;
                    }

                    editor.SelectedTool = tool;
                    msg.DisplayedMsg = "Edytor: " + text;
                    gui.SetLastTextColor(color);
                }
            }
        }

        public bool CheckPiecesLimit()
        {
            if (pieces.IsPiecesLimitReached)
            {
                msg.DisplayedMsg = "Limit 100 bierek został osiągnięty!";
                gui.SetLastTextColor(gui.LightGray);
                return true;
            }
            return false;
        }
    }
}
