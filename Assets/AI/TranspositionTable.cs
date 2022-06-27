namespace Laska
{
	/// <summary>
	/// Thanks to https://web.archive.org/web/20071031100051/http://www.brucemo.com/compchess/programming/hashing.htm
	/// and https://github.com/SebLague/Chess-AI/blob/d0832f8f1d32ddfb95525d1f1e5b772a367f272e/Assets/Scripts/Core/TranspositionTable.cs#L3
	/// </summary>
	public class TranspositionTable : MonoBehaviourExtended
	{
		[GlobalComponent] private Board board;
		
		public const float LookupFailed = float.MinValue;

		/// <summary>
		/// Value returned directly from <see cref="LaskaAI.EvaluatePosition(Player)"/>. (Not influenced by zugzwang-search) 
		/// </summary>
		public const byte Direct = 0;

		/// <summary>
		/// The value for this position is the exact evaluation.
		/// </summary>
		/// <remarks>
		/// Returned from PV-Node: this is a node with a score between alpha and beta. (Principal variation: a<s<b)
		/// Notice that it's not guaranteed to always be between [alpha, beta]. Exact score only means that
		/// it was not influenced by cutoffs. So this value will not change when the alpha/beta changes.
		/// </remarks>
		public const byte Exact = 1;

		/// <summary>
		/// A move was found during the search that was too good, meaning the opponent will play a different move earlier on,
		/// not allowing the position where this move was available to be reached. Because the search cuts off at
		/// this point (beta cut-off), an even better move may exist. This means that the evaluation for the
		/// position could be even higher, making the stored value the lower bound of the actual value.
		/// </summary>
		/// <remarks>
		/// Returned from Cut-Node: this is a node where a beta-cutoff occurs. (Fail-high: s>=b)
		/// Notice that it's not guaranteed to always be above beta, it only means that it was
		/// cached at some point where it was bigger than some old-beta. New-beta may be different.
		/// </remarks>
		public const byte LowerBound = 2;

		/// <summary>
		/// No move during the search resulted in a position that was better than the current player could get from playing a
		/// different move in an earlier position (i.e eval was <= alpha for all moves in the position).
		/// Because there could be beta-cutoffs deeper, the value we get here won't be the exact evaluation of the position,
		/// but rather the upper bound of the evaluation. This means that the evaluation is, at most, equal to this value.
		/// </summary>
		/// <remarks>
		/// Returned from All-Node: this is a node where alpha is not raised. (Fail-low: s<=a)
		/// Same as above, it's not guaranteed to be always lower than alpha. Alpha/beta values depend on previous positions we
		/// searched and TT-entires don't know previous positions (the idea is that we could reach the same position different way).
		/// </remarks>
		public const byte UpperBound = 3;

		/// <summary>
		/// Used when we want to store "bestMove" but not its value.
		/// </summary>
		public const byte Invalid = byte.MaxValue;

		public ulong size = 64000;
		public bool isEnabled = true;
		public bool failSoft;

		private Entry[] _entries;

		private void Start()
		{
			_entries = new Entry[size];
			//Debug.Log("TT size: " + (ulong)Entry.GetSize()*size);
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
			if (_entries[Index].key != board.ZobristKey)
				return null;

			return _entries[Index].move;
		}

		public float LookupDirectEvaluation(int plyFromRoot)
		{
			if (!isEnabled)
			{
				return LookupFailed;
			}
			Entry entry = _entries[Index];

			if (entry.key == board.ZobristKey && entry.nodeType == Direct)
			{
				return correctRetrievedWinEval(entry.value, plyFromRoot);
			}
			return LookupFailed;
		}

		public void StoreDirectEvaluation(int numPlySearched, float eval)
		{
			if (!isEnabled)
			{
				return;
			}

			var zobrist = board.ZobristKey;
			// Let's not overwrite "indirect" evals (as they are deeper)
			if (_entries[Index].key == zobrist)
				return;

			Entry entry = new Entry(zobrist, correctWinEvalForStorage(eval, numPlySearched), (sbyte)-1, Direct, null);
			_entries[Index] = entry;
		}

		public float LookupEvaluation(int depth, int plyFromRoot, float alpha, float beta, out string move)
		{
			move = null;
			if (!isEnabled)
			{
				return LookupFailed;
			}
			Entry entry = _entries[Index];

			if (entry.key == board.ZobristKey)
			{
				// Even if we don't get a score we can use, we can still use stored move to improve our move ordering
				move = entry.move;

				// Only use stored evaluation if it has been searched to at least the same depth as would be searched now
				if (entry.depth >= depth)
				{
					float correctedScore = correctRetrievedWinEval(entry.value, plyFromRoot);

					// Fail-soft vs fail-hard: https://stackoverflow.com/questions/72252975
					if (entry.nodeType == Exact)
					{
						// We have stored the exact evaluation for this position, so we can use it for any kind of node
						if (!failSoft)
						{
							// Cached at PV-Node but alpha-beta range could change
							if (correctedScore >= beta) return beta; // respect Fail-hard beta cutoff (Cut-Node)
							if (correctedScore <= alpha) return alpha; // Fail-hard fail-low (All-Node)
							// if it's within the window it's still PV-Node
						}
						return correctedScore; // in Fail-soft even if alpha-beta range changed we would still return "bestScore"
					}
					else if (entry.nodeType == UpperBound && correctedScore <= alpha)
					{
						// All-Node
						// We have stored the upper bound of the eval for this position. If it's less than alpha then we don't need to
						// search the moves in this position as they won't interest us; otherwise we will have to search to find the exact value
						return failSoft ? correctedScore : alpha; // in Fail-soft when we fail-low we don't clamp to alpha
					}
					else if (entry.nodeType == LowerBound && correctedScore >= beta)
					{
						// Cut-Node
						// We have stored the lower bound of the eval for this position. Only return if it causes a beta cut-off.
						return failSoft ? correctedScore : beta; // in Fail-soft when we fail-high we don't clamp to beta
					}
				}
			}
			return LookupFailed;
		}

		public void StoreEvaluation(int depth, int numPlySearched, float eval, byte evalType, string move)
		{
			if (!isEnabled)
			{
				return;
			}

			// We use Always Replace strategy (if we used more advanced one like Depth-Preferred then we would need to implement "Aging")
			Entry entry = new Entry(board.ZobristKey, correctWinEvalForStorage(eval, numPlySearched), (sbyte)depth, evalType, move);
			_entries[Index] = entry;
		}

		private float correctWinEvalForStorage(float eval, int numPlySearched)
		{
			if (LaskaAI.IsWinEval(eval))
			{
				int sign = System.Math.Sign(eval);
				return (eval * sign + numPlySearched) * sign;
			}
			return eval;
		}

		private float correctRetrievedWinEval(float eval, int numPlySearched)
		{
			if (LaskaAI.IsWinEval(eval))
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
			public readonly sbyte depth;
			public readonly byte nodeType;

			public Entry(ulong key, float value, sbyte depth, byte nodeType, string move)
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
