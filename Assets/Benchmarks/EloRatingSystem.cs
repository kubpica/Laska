using System;

public class EloRatingSystem
{
    private const int K = 24;

    /// <summary>
    /// Calculates Elo rating change for "player X".
    /// </summary>
    /// <param name="playerRating"> Rating of player X.</param>
    /// <param name="opponentRating"> Rating of his opponent.</param>
    /// <param name="result"> 0 if lost; 0.5 if draw; 1 if won.</param>
    /// <returns> The value by which the player's rating should be changed.</returns>
    public static double CalcChange(double playerRating, double opponentRating, double result)
    {
        double d = opponentRating - playerRating;
        if (d > 400) d = 400;
        else if (d < -400) d = -400;
        double we = 1 / (1 + Math.Pow(10, d / 400.0));
        double diff = result - we;
        return K * diff;
    }

    /// <summary>
    /// Updates rating
    /// </summary>
    /// <param name="ratingX"> Rating of X</param>
    /// <param name="ratingY"> Rating of Y</param>
    /// <param name="didXWin"> 1 if X won; 0.5 if draw; 0 if Y won</param>
    public static void UpdateRating(ref double ratingX, ref double ratingY, double didXWin)
    {
        var change = CalcChange(ratingX, ratingY, didXWin);
        ratingX += change;
        ratingY += change * -1;
    }
}