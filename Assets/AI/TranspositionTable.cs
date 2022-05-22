﻿namespace Laska
{
	/// <summary>
	/// Thanks to https://web.archive.org/web/20071031100051/http://www.brucemo.com/compchess/programming/hashing.htm
	/// and https://github.com/SebLague/Chess-AI/blob/d0832f8f1d32ddfb95525d1f1e5b772a367f272e/Assets/Scripts/Core/TranspositionTable.cs#L3
	/// </summary>
	public class TranspositionTable : MonoBehaviourExtended
	{
		[GlobalComponent] private Board board;
		
		public const float LookupFailed = float.MinValue;

		public const int None = 0;

		/// <summary>
		/// The value for this position is the exact evaluation.
		/// </summary>
		/// <remarks>
		/// Returned from PV-Node: this is a node with a score between alpha and beta. (Principal variation: a<s<b)
		/// </remarks>
		public const int Exact = 1;

		/// <summary>
		/// A move was found during the search that was too good, meaning the opponent will play a different move earlier on,
		/// not allowing the position where this move was available to be reached. Because the search cuts off at
		/// this point (beta cut-off), an even better move may exist. This means that the evaluation for the
		/// position could be even higher, making the stored value the lower bound of the actual value.
		/// </summary>
		/// <remarks>
		/// Returned from Cut-Node: this is a node where a beta-cutoff occurs. (Fail-high: s>=b)
		/// </remarks>
		public const int LowerBound = 2;

		/// <summary>
		/// No move during the search resulted in a position that was better than the current player could get from playing a
		/// different move in an earlier position (i.e eval was <= alpha for all moves in the position).
		/// Due to the way alpha-beta search works, the value we get here won't be the exact evaluation of the position,
		/// but rather the upper bound of the evaluation. This means that the evaluation is, at most, equal to this value.
		/// </summary>
		/// <remarks>
		/// Returned from All-Node: this is a node where alpha is not raised. (Fail-low: s<=a)
		/// </remarks>
		public const int UpperBound = 3; 

		public readonly ulong size = 64000;
		public bool isEnabled = true;

		private Entry[] _entries;

		private void Start()
		{
			_entries = new Entry[size];
		}

		public void Clear()
		{
			for (int i = 0; i < _entries.Length; i++)
			{
				_entries[i] = new Entry();
			}
		}

		public ulong Index
		{
			get
			{
				return board.ZobristKey % size;
			}
		}

		public string GetStoredMove()
		{
			return _entries[Index].move;
		}

		public float LookupEvaluation(int depth, int plyFromRoot, float alpha, float beta)
		{
			if (!isEnabled)
			{
				return LookupFailed;
			}
			Entry entry = _entries[Index];

			if (entry.key == board.ZobristKey)
			{
				// Only use stored evaluation if it has been searched to at least the same depth as would be searched now
				if (entry.depth >= depth)
				{
					float correctedScore = correctRetrievedWinEval(entry.value, plyFromRoot);
					if (entry.nodeType == Exact)
					{
						// We have stored the exact evaluation for this position, so return it
						return correctedScore;
					}
					else if (entry.nodeType == UpperBound && correctedScore <= alpha)
					{
						// We have stored the upper bound of the eval for this position. If it's less than alpha then we don't need to
						// search the moves in this position as they won't interest us; otherwise we will have to search to find the exact value
						return correctedScore;
					}
					else if (entry.nodeType == LowerBound && correctedScore >= beta)
					{
						// We have stored the lower bound of the eval for this position. Only return if it causes a beta cut-off.
						return correctedScore;
					}
				}
			}
			return LookupFailed;
		}

		public void StoreEvaluation(int depth, int numPlySearched, float eval, int evalType, string move)
		{
			if (!isEnabled)
			{
				return;
			}
			//ulong index = Index;
			//if (depth >= entries[Index].depth) {
			Entry entry = new Entry(board.ZobristKey, correctWinEvalForStorage(eval, numPlySearched), (byte)depth, (byte)evalType, move);
			_entries[Index] = entry;
			//}
		}

		private bool isWinEval(float eval) => eval == LaskaAI.ACTIVE_WIN || eval == LaskaAI.INACTIVE_WIN;

		private float correctWinEvalForStorage(float eval, int numPlySearched)
		{
			if (isWinEval(eval))
			{
				int sign = System.Math.Sign(eval);
				return (eval * sign + numPlySearched) * sign;
			}
			return eval;
		}

		private float correctRetrievedWinEval(float eval, int numPlySearched)
		{
			if (isWinEval(eval))
			{
				int sign = System.Math.Sign(eval);
				return (eval * sign - numPlySearched) * sign;
			}
			return eval;
		}

		public struct Entry
		{
			public readonly ulong key;
			public readonly float value;
			public readonly string move;
			public readonly byte depth;
			public readonly byte nodeType;

			public Entry(ulong key, float value, byte depth, byte nodeType, string move)
			{
				this.key = key;
				this.value = value;
				this.depth = depth; // depth is how many ply were searched ahead from this position
				this.nodeType = nodeType;
				this.move = move;
			}

			public static int GetSize()
			{
				return System.Runtime.InteropServices.Marshal.SizeOf<Entry>();
			}
		}
	}
}