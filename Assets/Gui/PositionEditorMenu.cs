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
        [GlobalComponent] private ThemeManager theme;

        private Language Language => LanguageManager.Language;

        private void OnEnable()
        {
            msg.DisplayedMsg = $"{Language.editor}: {Language.deleting}";
            gui.SetLastTextColor(gui.LightGray);
        }

        private void OnDisable()
        {
            msg.DisplayedMsg = "";
        }

        private void OnGUI()
        {
            if (gui.ButtonTopRight(new Rect(340, 10, 305, 80), $"{Language.exit} {Language.editor.ToLower()}"))
            {
                // Save postion & exit editor
                fen.SaveCurrentPosition();
                menus.ShowIngameMenu();
                fen.Reload();
            }

            if (gui.ButtonTopRight(new Rect(340, 100, 305, 80), Language.restoreDefault))
            {
                // Restore default postion
                fen.RestoreDefaultPostion();
            }

            bool isWhite = game.ActivePlayer.color == 'w';
            if (gui.ButtonTopRight(new Rect(340, 190, 305, 80), isWhite ? Language.greenToMove : Language.redToMove,
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
            gui.SoundButton(770);
        }

        private string getToolName(PositionEditorTool tool)
        {
            switch (tool)
            {
                case PositionEditorTool.Delete:
                    return Language.deleting;
                case PositionEditorTool.GreenSoldier:
                    return $"{Language.green} {theme.Soldier.ToLower()}";
                case PositionEditorTool.GreenOfficer:
                    return $"{Language.green} {theme.Officer.ToLower()}";
                case PositionEditorTool.RedSoldier:
                    return $"{Language.red} {theme.Soldier.ToLower()}";
                case PositionEditorTool.RedOfficer:
                    return $"{Language.red} {theme.Officer.ToLower()}";
            }
            return "";
        }

        private Color getToolColor(PositionEditorTool tool)
        {
            switch (tool)
            {
                case PositionEditorTool.Delete:
                    return gui.LightGray;
                case PositionEditorTool.GreenSoldier:
                case PositionEditorTool.GreenOfficer:
                    return Color.green;
                case PositionEditorTool.RedSoldier:
                case PositionEditorTool.RedOfficer:
                    return Color.red;
            }
            return gui.ButtonStyle.normal.textColor;
        }

        public void UpdateToolText()
        {
            menus.msg.DisplayedMsg = $"{Language.editor}: {getToolName(editor.SelectedTool)}";
            gui.SetLastTextColor(getToolColor(editor.SelectedTool));
        }

        private void toolSelection()
        {
            toolButton(0, PositionEditorTool.Delete);
            if (pieces.IsPiecesLimitReached)
                return;

            toolButton(1, PositionEditorTool.GreenSoldier);
            toolButton(2, PositionEditorTool.GreenOfficer);
            toolButton(3, PositionEditorTool.RedSoldier);
            toolButton(4, PositionEditorTool.RedOfficer);

            void toolButton(int i, PositionEditorTool tool)
            {
                var deselectedColor = Color.Lerp(gui.ButtonStyle.normal.textColor, getToolColor(tool), 0.15f);
                if (gui.ButtonTopRight(new Rect(315, 300 + 90 * i, 280, 80), getToolName(tool), editor.SelectedTool == tool, deselectedColor))
                {
                    if (CheckPiecesLimit())
                    {
                        editor.SelectedTool = PositionEditorTool.Delete;
                        return;
                    }

                    editor.SelectedTool = tool;
                }
            }
        }

        public bool CheckPiecesLimit()
        {
            if (pieces.IsPiecesLimitReached)
            {
                msg.DisplayedMsg = Language.piecesLimitReached;
                gui.SetLastTextColor(gui.LightGray);
                return true;
            }
            return false;
        }
    }
}
