using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Laska
{
	public static class Zobrist
	{
		const int seed = 4066935;
		const string randomNumbersFileName = "RandomNumbers.txt";

		/// piece type, square index, height in the column
		public static readonly ulong[,,] piecesArray = new ulong[4, 25, 22];
		public static readonly ulong blackToMove;

		static System.Random prng = new System.Random(seed);

		static void WriteRandomNumbers()
		{
			prng = new System.Random(seed);
			string randomNumberString = "";
			int numRandomNumbers = 4 * 25 * 22 + 1;

			for (int i = 0; i < numRandomNumbers; i++)
			{
				randomNumberString += RandomUnsigned64BitNumber();
				if (i != numRandomNumbers - 1)
				{
					randomNumberString += ',';
				}
			}
			var writer = new StreamWriter(randomNumbersPath);
			writer.Write(randomNumberString);
			writer.Close();
		}

		static Queue<ulong> ReadRandomNumbers()
		{
			if (!File.Exists(randomNumbersPath))
			{
				Debug.Log("Create");
				WriteRandomNumbers();
			}
			Queue<ulong> randomNumbers = new Queue<ulong>();

			var reader = new StreamReader(randomNumbersPath);
			string numbersString = reader.ReadToEnd();
			reader.Close();

			string[] numberStrings = numbersString.Split(',');
			for (int i = 0; i < numberStrings.Length; i++)
			{
				ulong number = ulong.Parse(numberStrings[i]);
				randomNumbers.Enqueue(number);
			}
			return randomNumbers;
		}

		static Zobrist()
		{
			var randomNumbers = ReadRandomNumbers();

			for (int pieceIndex = 0; pieceIndex < 4; pieceIndex++)
			{
				for (int squareIndex = 0; squareIndex < 25; squareIndex++)
				{
					for (int columnHeight = 0; columnHeight < 22; columnHeight++)
					{
						piecesArray[pieceIndex, squareIndex, columnHeight] = randomNumbers.Dequeue();
					}
				}
			}	
			blackToMove = randomNumbers.Dequeue();
		}

		/// <summary>
		/// Calculate zobrist key from current board position. This should only be used after setting board from fen; during search the key should be updated incrementally.
		/// </summary>
		public static ulong CalcZobristKey(char colorToMove)
		{
			ulong zobristKey = 0;

			var board = Board.Instance;
			foreach (var player in GameManager.Instance.players)
			{
				foreach (var piece in player.pieces)
				{
					zobristKey ^= piecesArray[piece.ZobristIndex, piece.Square.draughtsNotationIndex-1, piece.GetHeightInColumn()];
				}
			}

			if (colorToMove == 'b')
			{
				zobristKey ^= blackToMove;
			}

			return zobristKey;
		}

		static string randomNumbersPath
		{
			get
			{
				return Path.Combine(Application.streamingAssetsPath, randomNumbersFileName);
			}
		}

		static ulong RandomUnsigned64BitNumber()
		{
			byte[] buffer = new byte[8];
			prng.NextBytes(buffer);
			return BitConverter.ToUInt64(buffer, 0);
		}
	}
}
