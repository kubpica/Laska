using System.Collections.Generic;

/// <summary>
/// https://pl.wikipedia.org/wiki/System_ko%C5%82owy#Tabele_Bergera_jako_graf
/// </summary>
public class BergsCircle
{
    public static List<MatchPair> GetPairs(List<TournamentBot> competitors, int tournamentStage)
    {
        BergsCircle circle = new BergsCircle(competitors);
        while (circle.Stage != tournamentStage)
        {
            circle.NextStage();
        }
        return circle.GetPairs();
    }

    public int Stage { get; set; } = 1;

    private List<TournamentBot> _slots = new List<TournamentBot>();

    public BergsCircle(List<TournamentBot> competitors)
    {
        _slots.Capacity = competitors.Count;

        foreach (var c in competitors)
        {
            _slots.Insert(c.StartingPosition - 1, c);
        }

        if (_slots.Count % 2 == 1)
        {
            _slots.Add(null);
        }
    }

    /// <summary>
    /// Reads current pairings from the circle.
    /// </summary>
    /// <returns> Matches in current stage.</returns>
    public List<MatchPair> GetPairs()
    {
        var pairs = new List<MatchPair>();
        // Players on corresponding vertices (slots) are paired with each other
        // https://upload.wikimedia.org/wikipedia/commons/thumb/8/8e/Round-robin-schedule-span-diagram.svg/1024px-Round-robin-schedule-span-diagram.svg.png
        for (int i = 0; i < _slots.Count / 2; i++)
        {
            var p1 = _slots[i];
            var p2 = _slots[_slots.Count - 1 - i];

            // Player null means bye - his opponent pauses this turn (has no match at this stage)
            if (p1 == null || p2 == null)
                continue;

            pairs.Add(new MatchPair(p1, p2));
        }
        return pairs;
    }
    
    /// <summary>
    /// Set the circle so that it indicates pairs in the next stage.
    /// </summary>
    public void NextStage()
    {
        // The pairs of the next stage are obtained by moving the numbers of the vertices (except for the middle vertex) clockwise
        var circle = new List<TournamentBot>(_slots);
        circle.Capacity = _slots.Count;

        // One (last) participant is stationary on the wheel, and the rest are moved one position
        circle[_slots.Count - 1] = _slots[_slots.Count - 1];
        circle[0] = _slots[_slots.Count - 2];
        for (int i = 0; i < _slots.Count - 2; i++)
        {
            circle[i + 1] = _slots[i];
        }
        _slots = circle;
        Stage++;
    }
}