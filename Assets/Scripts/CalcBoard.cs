using System.Linq;
using UnityEngine;

namespace Laska
{
    public class CalcBoard : MonoBehaviourExtended
    {
        [GlobalComponent] private Board board;

        private float[] _inSquares = new float[25] { 15, 19, 19, 15, 14, 16, 14, 8, 12, 12, 8, 7, 9, 7, 3, 5, 5, 3, 2, 2, 2, 24, 24, 24, 24 };
        private float[] _outSquares;
        private float[] _deadSquares;

        private float _summed;

        private void Start()
        {
            calcTakenSquaresEvForAllSquares();
            //calcSoldierValueOnSquares();

            calcSquaresSoldierOccupation();
        }

        private void calcSoldierValueOnSquares()
        {
            for (int i = 1; i <= 25; i++)
            {
                calcSoldierValueOnSquare(board.GetSquareAt(i));
            }
            Debug.Log("Avg result " + _summed / 25f);
        }

        private void calcSoldierValueOnSquare(Square s)
        {
            _outSquares = new float[25];
            _deadSquares = new float[25];

            visitSquare(s, 1, true, true, false);
            //print(_outSquares);

            calcSimpleValue(s);
            //Debug.Log("Result(" + s.draughtsNotationIndex + "):");
            //calcOverallValue();
        }

        private void calcSimpleValue(Square s)
        {
            //board.GetSquareIds(s.coordinate, out _, out int rank);
            float divider = _outSquares.Sum(); //7 - rank;

            multiplySquares();

            var result = _outSquares.Sum() / divider;
            Debug.Log("Result(" + s.draughtsNotationIndex + "): " + result);
            _summed += result;
        }

        private void calcSquaresSoldierOccupation()
        {
            _outSquares = new float[25];
            _deadSquares = new float[25];

            // First 3 starting rows
            for (int i = 1; i <= 11; i++)
            {
                var s = board.GetSquareAt(i);
                visitSquare(s, 1, false, false, false);
            }

            // "Unsafe" positions - squares where a piece can be released
            for (int rank = 1; rank < 6; rank++)
            {
                for (int file = 1; file < 6; file++)
                {
                    var s = board.GetSquareAt(file, rank);
                    if (s.draughtsNotationIndex == 0)
                        continue;
                    visitSquare(s, 1, false, false, false);
                }
            }

            Debug.Log("SquaresSoldierOccupation:");
            print(_outSquares);

            calcOverallValue();
        }

        private void calcOverallValue()
        {
            float normalWeight = 0;
            for (int i = 0; i < 21; i++)
                normalWeight += _outSquares[i];

            float promotionWeight = 0;
            for (int i = 21; i < 25; i++)
                promotionWeight += _outSquares[i];

            float deathWeight = _deadSquares.Sum();
            float totalWeight = normalWeight + promotionWeight + deathWeight;

            multiplySquares();

            float normalValue = 0;
            for (int i = 0; i < 21; i++)
                normalValue += _outSquares[i];

            var result = normalWeight == 0 ? 0 : normalValue / normalWeight * (normalWeight / totalWeight);
            var officerP = promotionWeight / totalWeight;
            var captivesP = deathWeight / totalWeight;
            //Debug.Log("Result: " + result + " + " + officerP + "%Officer + " + captivesP + "%Captives");

            // Officer value
            result += officerP * 10.296f;
            captivesP += officerP * 0.208f;
            Debug.Log("Result: " + result + " + " + captivesP + "%Captives " + "(" + (result - captivesP) + ")");
        }

        private void print(float[] array)
        {
            string s = "";
            foreach (var f in array)
                s += f + " ";
            Debug.Log(s);
        }

        private void calcTakenSquaresEvForAllSquares()
        {
            for (int i = 0; i < 25; i++)
            {
                var ev = calcTakenSquaresEv((int)_inSquares[i], 24);
                Debug.Log("EV" + i + ": " + ev);
                _inSquares[i] -= ev;
            }
        }

        private float calcTakenSquaresEv(int allTargetSquares, int allFreeSquares)
        {
            int maxDepth = 11;
            var probabilities = new float[allTargetSquares + 1];
            visitNode(allTargetSquares, allFreeSquares, 0, 0, 1);
            float ev = 0;
            for (int i = 0; i < allTargetSquares + 1; i++)
            {
                ev += i * probabilities[i];
                //Debug.Log("Probability" + i + ": " + probabilities[i]);
            }
            return ev;

            void visitNode(int targetSquaresLeft, int freeSquaresLeft, int depth, int hits, float probability)
            {
                if (depth == maxDepth || targetSquaresLeft == 0)
                {
                    probabilities[hits] += probability;
                    return;
                }

                // Hit target square
                var hitP = targetSquaresLeft / (float)freeSquaresLeft;
                visitNode(targetSquaresLeft - 1, freeSquaresLeft - 1, depth + 1, hits + 1, probability * hitP);

                // Miss
                var missP = 1 - hitP;
                visitNode(targetSquaresLeft, freeSquaresLeft - 1, depth + 1, hits, probability * missP);
            }
        }

        private void multiplySquares()
        {
            for (int i = 0; i < 25; i++)
            {
                _outSquares[i] *= _inSquares[i];
            }
        }

        private void visitSquare(Square s, float value, bool progressiveSplit, bool forkSplit, bool firstSafe = false)
        {
            board.GetSquareIds(s.coordinate, out int file, out int rank);

            // Check if the square is not safe (not near border)
            if (!firstSafe && file != 0 && file != 6 && rank != 0 && rank != 6) //
            {
                // 60% for surviving
                var deadValue = value * 0.4f;
                _deadSquares[s.draughtsNotationIndex - 1] += deadValue;
                value -= deadValue;
            }

            var left = getSquare(file - 1, rank + 1);
            var right = getSquare(file + 1, rank + 1);

            if (progressiveSplit)
            {
                // Half value for this square, rest for next squares
                if (left != null || right != null)
                    value *= 0.5f;
            }

            _outSquares[s.draughtsNotationIndex - 1] += value;

            // Split value if there are 2 next squares available
            if (forkSplit && left != null && right != null)
            {
                value /= 2.0f;
            }

            if (left != null)
                visitSquare(left, value, progressiveSplit, forkSplit);
            if (right != null)
                visitSquare(right, value, progressiveSplit, forkSplit);
        }

        private Square getSquare(int file, int rank)
        {
            try
            {
                return board.GetSquareAt(file, rank);
            }
            catch
            {
                return null;
            }
        }
    }
}