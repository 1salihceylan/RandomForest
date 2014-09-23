using System;
using YarrLib;

namespace RandomForest
{
	public class Classifier
	{
		private IScorer Scorer;
		public double Cutoff = 1.0;

		public Classifier(IScorer scorer)
		{
			this.Scorer = scorer;
		}

		public char[] Classify(RecordSet data, bool parallel=true)
		{
			Score scores = this.Scorer.Score(data, parallel);

			double[] ratios = Yarr.Div(scores.SScores, scores.BScores);

			char[] result = new char[data.NRows];
			for (int i=0; i<data.NRows; i++)
			{
				result[i] = (ratios[i] >= this.Cutoff) ? 's' : 'b';
			}
			return result;
		}
	}
}

