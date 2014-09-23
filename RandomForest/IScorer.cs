using System;

namespace RandomForest
{
	public class Score
	{
		public readonly double[] SScores;
		public readonly double[] BScores;

		public Score(double[] sScores, double[] bScores)
		{
			this.SScores = sScores;
			this.BScores = bScores;
		}
	}

	public interface IScorer
	{
		Score Score(RecordSet data, bool parallel=true);
	}
}

