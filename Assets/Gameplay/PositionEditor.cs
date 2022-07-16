using UnityEngine;

namespace Laska
{
    public class PositionEditor : MonoBehaviourSingleton<PositionEditor>
    {
        private abstract class EditorTool
        {
            public abstract void Do(Square square);
        }

        private class DeleteTool : EditorTool
        {
            public override void Do(Square square)
            {
                PiecesManager.Instance.RemovePiece(square);
            }
        }

        private class PieceTool : EditorTool
        {
            private char _piece;

            public PieceTool(char piece)
            {
                _piece = piece;
            }

            public override void Do(Square square)
            {
                PiecesManager.Instance.AddPiece(_piece, square);
            }
        }

        [GlobalComponent] CameraController cameraController;
        [GlobalComponent] PiecesManager pieces;

        private Camera _cam;
        private Square _selectedSquare;
        
        private DeleteTool _delete;
        private PieceTool _greenSoldier;
        private PieceTool _greenOfficer;
        private PieceTool _redSoldier;
        private PieceTool _redOfficer;

        public enum PositionEditorTool
        {
            Delete,
            GreenSoldier,
            GreenOfficer,
            RedSoldier,
            RedOfficer
        }

        private EditorTool _currentTool;
        private PositionEditorTool _selectedTool;
        public PositionEditorTool SelectedTool
        {
            get => _selectedTool;
            set
            {
                _selectedTool = value;

                switch (_selectedTool)
                {
                    case PositionEditorTool.Delete:
                        _currentTool = _delete;
                        break;
                    case PositionEditorTool.GreenSoldier:
                        _currentTool = _greenSoldier;
                        break;
                    case PositionEditorTool.GreenOfficer:
                        _currentTool = _greenOfficer;
                        break;
                    case PositionEditorTool.RedSoldier:
                        _currentTool = _redSoldier;
                        break;
                    case PositionEditorTool.RedOfficer:
                        _currentTool = _redOfficer;
                        break;
                }
            }
        }

        private void Start()
        {
            _cam = cameraController.Camera;

            _delete = new DeleteTool();
            _greenSoldier = new PieceTool('w');
            _greenOfficer = new PieceTool('W');
            _redSoldier = new PieceTool('b');
            _redOfficer = new PieceTool('B');
            SelectedTool = PositionEditorTool.Delete;
        }

        private void Update()
        {
            detectSquareHover();
        }

        private void selectSquare(Square square)
        {
            if (_selectedSquare == square)
                return;

            _selectedSquare = square;

            if(square != null && square.draughtsNotationIndex > 0)
            {
                _currentTool.Do(square);
                if (pieces.IsPiecesLimitReached)
                    MenusManager.Instance.editor.CheckPiecesLimit();
            }
        }

        private void detectSquareHover()
        {
            if (Input.GetMouseButton(0) || Input.touchCount > 0)
            {
                var underMouse = _cam.GetColliderUnderMouse();
                if (underMouse != null)
                {
                    if (underMouse.gameObject.CompareTag("Board"))
                    {
                        var square = underMouse.GetComponent<Square>();
                        if (square != null)
                        {
                            selectSquare(square);
                            return;
                        }
                    }
                    else
                    {
                        var piece = underMouse.GetComponent<Piece>();
                        if (piece != null)
                        {
                            selectSquare(piece.Square);
                            return;
                        }
                    }
                }
            }
            else
            {
                selectSquare(null);
            }
        }
    }
}

