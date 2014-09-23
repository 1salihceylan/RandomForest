using System;

namespace RandomForest
{
	public class ScoreCacher : IScorer
	{
		private IScorer ToCache;
		private Score Cache;

		public ScoreCacher(IScorer tocache)
		{
			this.ToCache = tocache;
		}

		public Score Score(RecordSet data, bool parallel = true)
		{
			if (this.Cache == null)
			{
				this.Cache = this.ToCache.Score(data, parallel);
			}
			return this.Cache;
		}
	}
}

