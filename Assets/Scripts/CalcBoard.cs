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

        private void Start()
        {
            calcTakenSquaresEvForAllSquares();
            print(_inSquares);
            calcSoldierValueOnSquares();
            //calcSquaresSoldierOccupation();
        }

        private void calcSoldierValueOnSquares()
        {
            var results = new float[25];
            var captives = new float[25];
            for (int i = 0; i < 25; i++)
            {
                calcSoldierValueOnSquare(board.GetSquareAt(i+1), out float result, out float captivesP);
                results[i] = result;
                captives[i] = captivesP;
            }

            _outSquares = new float[25];
            _deadSquares = new float[25];

            // First 3 starting rows
            for (int i = 1; i <= 11; i++)
            {
                var s = board.GetSquareAt(i);
                visitSquare(s, 1, false, false, false);
            }

            // "Unsafe" positions - squares where a piece can be released
            var deathsPerUnsafeSquare = _deadSquares.Sum() / 13.0f * 0.75f; //1;
            Debug.Log("deathsPerUnsafeSquare: " + deathsPerUnsafeSquare);
            for (int rank = 1; rank < 6; rank++)
            {
                for (int file = 1; file < 6; file++)
                {
                    var s = board.GetSquareAt(file, rank);
                    if (s.draughtsNotationIndex == 0)
                        continue;
                    visitSquare(s, deathsPerUnsafeSquare, false, false, false);
                }
            }

            multiplySquares(ref results, ref _outSquares);
            multiplySquares(ref captives, ref _outSquares);

            var normalWeight = _outSquares.Sum();
            var deathWeight = _deadSquares.Sum();
            var totalWeight = normalWeight + deathWeight;
            var normalP = normalWeight / totalWeight;
            var deathP = deathWeight / totalWeight;

            var avgResult = results.Sum() / normalWeight * normalP;
            var avgCaptivesP = captives.Sum() / normalWeight * normalP + deathP;
            Debug.Log("AvgResult: " + avgResult + " + " + avgCaptivesP + "%Captives " + "(" + (avgResult - avgCaptivesP) + ")");
        }

        private void calcSoldierValueOnSquare(Square s, out float result, out float captivesP)
        {
            _outSquares = new float[25];
            _deadSquares = new float[25];

            visitSquare(s, 1, true, true, false);
            //print(_outSquares);

            //calcSimpleValue(s);
            Debug.Log("Result(" + s.draughtsNotationIndex + "):");
            calcOverallValue(out result, out captivesP);
        }

        private void calcSimpleValue(Square s)
        {
            //board.GetSquareIds(s.coordinate, out _, out int rank);
            float divider = _outSquares.Sum(); //7 - rank;

            multiplySquares();

            var result = _outSquares.Sum() / divider;
            Debug.Log("Result(" + s.draughtsNotationIndex + "): " + result);
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

            calcOverallValue(out _, out _);
        }

        private void calcOverallValue(out float result, out float captivesP)
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

            result = normalWeight == 0 ? 0 : normalValue / normalWeight * (normalWeight / totalWeight);
            var officerP = promotionWeight / totalWeight;
            captivesP = deathWeight / totalWeight;
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

        private void multiplySquares(ref float[] outSquares, ref float[] inSquares)
        {
            for (int i = 0; i < outSquares.Length; i++)
            {
                outSquares[i] *= inSquares[i];
            }
        }

        private void multiplySquares()
        {
            for (int i = 0; i < 25; i++)
            {
                _outSquares[i] *= _inSquares[i];
            }
        }

        private void visitSquare(Square s, float value, bool progressiveSplit, bool forkSplit,
            bool firstSafe = false, float deathChance = 0.4f)
        {
            board.GetSquareIds(s.coordinate, out int file, out int rank);

            // Check if the square is not safe (not near border)
            if (!firstSafe && file != 0 && file != 6 && rank != 0 && rank != 6)
            {
                // 60% for surviving
                var deadValue = value * deathChance;
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
                visitSquare(left, value, progressiveSplit, forkSplit, false, deathChance);
            if (right != null)
                visitSquare(right, value, progressiveSplit, forkSplit, false, deathChance);
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