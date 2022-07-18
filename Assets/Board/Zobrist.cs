using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Laska
{
	public static class Zobrist
	{
		private const int SEED = 4066935;
		private const string FILE_NAME = "RandomNumbers.txt";
		private const int PIECE_TYPES = 4;
		private const int SQUARES = 25;
		private const int MAX_PIECES = 100; // Normally 22 would be enough, but in position editor we allow max 100 pieces 

		private static int NumbersToGenerate => PIECE_TYPES * SQUARES * MAX_PIECES + 1;

		/// <summary>
		/// [piece type, square index, height in the column]
		/// </summary>
		public static readonly ulong[,,] piecesArray = new ulong[PIECE_TYPES, SQUARES, MAX_PIECES];
		public static readonly ulong blackToMove;

		static System.Random prng = new System.Random(SEED);

		static void WriteRandomNumbers()
		{
			prng = new System.Random(SEED);
			string randomNumberString = "";
			int numRandomNumbers = NumbersToGenerate;

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
			if (numberStrings.Length < NumbersToGenerate)
			{
				Debug.LogError("Not enought numberStrings, regenerating...");
				File.Delete(randomNumbersPath);
				return ReadRandomNumbers();
			}

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

			for (int pieceIndex = 0; pieceIndex < PIECE_TYPES; pieceIndex++)
			{
				for (int squareIndex = 0; squareIndex < SQUARES; squareIndex++)
				{
					for (int columnHeight = 0; columnHeight < MAX_PIECES; columnHeight++)
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
				return Path.Combine(Application.persistentDataPath, FILE_NAME);
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
