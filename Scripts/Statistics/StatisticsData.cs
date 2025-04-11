namespace CosmocrushGD
{
    public class StatisticsData
    {
        public int GamesPlayed { get; set; } = 0;
        public long TotalScore { get; set; } = 0; // Use long for total score to prevent overflow
        public int TopScore { get; set; } = 0;

        // Average score can be calculated dynamically: TotalScore / GamesPlayed (handle division by zero)
    }
}