using System.Collections.Generic;
using UnityEngine;

namespace Laska
{
	public class MoveOrdering : MonoBehaviourExtended
	{
		[GlobalComponent] private Board board;
		private float[] _moveScores = new float[44];

		public bool evalStrengthByRisk;
		public bool evalTakesByRisk;

		public void OrderMoves(List<string> moves, string ttMove)
		{
			bool canTake = moves[0].Length > 5;

			for (int i = 0; i < moves.Count; i++)
			{
				string move = moves[i];
				
				float score = 0;

				if (canTake)
				{
					if (evalTakesByRisk)
					{
						// Prefer more valuable takes (taking officers, releasing captives etc)
						for (int j = 3; j < move.Length; j += 6)
						{
							var takenColumn = board.GetColumnAt(move.Substring(j, 2));
							score += takenColumn.Risk;
						}
					}
					else
					{
						// Prefer longer takes
						score += move.Length;
					}
					score *= 100000;
				}

				// Prefer moving stronger columns
				var column = board.GetColumnAt(move.Substring(0, 2));
				if (evalStrengthByRisk)
				{
					score += (100 - column.Risk) * 1000;
				}
				else
				{
					score += column.Strength * 10;
				}

				//TODO Prefer moves close to other pieces

				// Prefer moves closer to the center
				score += 3 - distanceToCenter(move.Substring(move.Length - 2));

				if (move == ttMove)
					score += 10000;

				_moveScores[i] = score;
			}

			sort(moves);
		}

		private int distanceToCenter(string square)
		{
			board.GetSquareIds(square, out int fileId, out int rankId);
			return Mathf.Max(Mathf.Abs(3 - fileId), Mathf.Abs(3 - rankId));
		}

		private void sort(List<string> moves)
		{
			// Sort the moves list based on scores
			for (int i = 0; i < moves.Count - 1; i++)
			{
				for (int j = i + 1; j > 0; j--)
				{
					int swapIndex = j - 1;
					if (_moveScores[swapIndex] < _moveScores[j])
					{
						(moves[j], moves[swapIndex]) = (moves[swapIndex], moves[j]);
						(_moveScores[j], _moveScores[swapIndex]) = (_moveScores[swapIndex], _moveScores[j]);
					}
				}
			}
		}
	}
}