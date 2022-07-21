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
                if (square.IsEmpty)
                    return;

                var p = square.Column.Commander.transform.position;
                PiecesManager.Instance.RemovePiece(square);
                AudioManager.Instance.PlayAtPoint("Capture", p);
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
                AudioManager.Instance.PlayAtPoint("Placement", square.Column.Commander.transform.position);
            }
        }

        [GlobalComponent] private CameraController cameraController;
        [GlobalComponent] private PiecesManager pieces;
        [GlobalComponent] private MenusManager menus;

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

                menus.editor.UpdateToolText();
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
            detectSquareClick();
            detectToolChangeOnMouse();
        }

        private void selectSquare(Square square)
        {
            if (_selectedSquare == square)
                return;

            _selectedSquare = square;

            if(square != null && square.draughtsNotationIndex > 0)
            {
                _currentTool.Do(square);
                menus.msg.UpdateEval();
                if (pieces.IsPiecesLimitReached)
                    menus.editor.CheckPiecesLimit();
            }
        }

        private bool getSquareUnderMouse(out Square square)
        {
            var underMouse = _cam.GetColliderUnderMouse();
            if (underMouse != null)
            {
                if (underMouse.gameObject.CompareTag("Board"))
                {
                    square = underMouse.GetComponent<Square>();
                    return true;
                }
                else
                {
                    square = underMouse.GetComponent<Piece>().Square;
                    return true;
                }
            }
            square = null;
            return false;
        }

        private void altAction(Square square)
        {
            if (square.draughtsNotationIndex == 0)
                return;

            if (square.IsEmpty)
            {
                _currentTool.Do(square);
            }
            else if (_currentTool == _delete)
            {
                var c = square.Column.Commander;
                EditorTool tempTool;
                if (c.IsOfficer)
                {
                    tempTool = c.Color == 'w' ? _greenSoldier : _redSoldier;
                }
                else
                {
                    tempTool = c.Color == 'w' ? _greenOfficer : _redOfficer;
                }

                _delete.Do(square);
                tempTool.Do(square);
            }
            else
            {
                _delete.Do(square);
            }
            menus.msg.UpdateEval();
        }

        private void detectToolChangeOnMouse()
        {
            if (Input.GetMouseButtonDown(3))
            {
                SelectedTool = (PositionEditorTool)((int)(SelectedTool + 1) % 5);
            }
            else if (Input.GetMouseButtonDown(4))
            {
                if (SelectedTool == PositionEditorTool.Delete)
                {
                    SelectedTool = PositionEditorTool.RedOfficer;
                    return;
                }
                SelectedTool = (PositionEditorTool)((int)(SelectedTool - 1) % 5);
            }
        }

        private void detectSquareClick()
        {
            if (Input.GetMouseButton(0) || Input.touchCount > 0)
            {
                if(getSquareUnderMouse(out Square s))
                {
                    selectSquare(s);
                }
            }
            else if (Input.GetMouseButtonDown(2))
            {
                if (getSquareUnderMouse(out Square s))
                {
                    altAction(s);
                }
            }
            else
            {
                selectSquare(null);
            }
        }
    }
}

