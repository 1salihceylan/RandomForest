using System;
using System.Collections.Generic;
using System.Linq;
using YarrLib;

namespace RandomForest
{
	public class Tree : IScorer
	{
		private static Random RNG = new Random();

		private double SDensity;
		private double BDensity;

		private int NDim;
		private int[] TargetFeatures;

		private double[] MinCorner;
		private double[] MaxCorner;

		private bool[] IncludeMax;

		private double NormalizingConstant;

		private TrainingRecordSet TrainPoints;
		private int NTrainPoints;

		private List<Tree> Children;

		/// <summary>
		/// Assuming this Tree has been Train()ed, this
		/// is the *local* index of the dimension that
		/// we split on. That is, it is an index to
		/// <code>this.TargetFeatures</code>, not an index to 
		/// <code>this.TrainPoints.FeatureCols</code>. The conversion
		/// to a global index that would work there is
		/// <code>this.TargetFeatures[this.SplitDim]</code>
		/// </summary>
		private int SplitDim;
		private double SplitVal;

		public Tree(
			TrainingRecordSet trainPoints,
			int[] targetFeatures,
			double[] minCorner,
			double[] maxCorner,
			bool[] includeMax=null,
			double? normalizingConstant=null
		)
		{
			this.TargetFeatures = targetFeatures;
			int ndim = this.NDim = targetFeatures.Length;

			this.TrainPoints = trainPoints;
			int npoints = this.NTrainPoints = trainPoints.NRows;

			this.MinCorner = minCorner;
			this.MaxCorner = maxCorner;

			if (includeMax == null)
			{
				includeMax = Yarr.Repeat(true, ndim);
			}
			this.IncludeMax = includeMax;

			if (!normalizingConstant.HasValue)
			{
				normalizingConstant = npoints;
			}
			this.NormalizingConstant = normalizingConstant.Value;

			double normalizedVolume = (CalcVolume() * this.NormalizingConstant);
			int nS = trainPoints.Labels.CountEqu('s');
			int nB = npoints - nS;
			this.SDensity = nS / normalizedVolume;
			this.BDensity = nB / normalizedVolume;
		}

		private double CalcVolume()
		{
			double volume = 1.0;
			for (int i=0; i<this.NDim; i++)
			{
				volume *= Math.Abs(this.MaxCorner[i] - this.MinCorner[i]);
			}
			return volume;
		}

		public void Train(int? maxDepth=null, int? minPts=null)
		{
			int sqrt = (int)(Math.Floor(Math.Sqrt(this.NTrainPoints)));
			//int root = (int)(Math.Floor(Math.Pow(this.NTrainPoints, 1.0 / this.NDim)));

			int _maxDepth = maxDepth ?? sqrt;
			int _minPts = minPts ?? sqrt; //TODO - figure out better heuristic

			_Train(_maxDepth, _minPts);
		}

		private void _Train(int maxDepth, int minPts)
		{
			bool tooSmall = (maxDepth <= 0 || this.NTrainPoints <= minPts);
			if (!tooSmall)
			{
				this.Children = Split();
				if (this.Children != null) //sometimes we will abort mid-split if we can't find anything good
				{
					foreach (Tree child in this.Children)
					{
						child._Train(maxDepth - 1, minPts);
					}
				}
			}
			this.TrainPoints = null; //may help with memory utilization
		}

		private double Entropy(params int[] setCounts)
		{
			double sum = (double)Yarr.Sum(setCounts);
			if (sum == 0.0)
			{
				return 0.0;
			}

			double nent = 0.0;
			for (int i=0; i<setCounts.Length; i++)
			{
				if(setCounts[i] == 0)
				{
					continue; // prop*logProp == 0 * -inf == NaN even though it should be 0. Better to be lazy and skip it.
				}
				double prob = setCounts[i] / sum;
				double logProb = Math.Log(prob, 2.0);
				nent += prob * logProb;
			}
			return -nent;
		}

        private double? _TotalEntropy;
		private double TotalEntropy()
		{
            if (!_TotalEntropy.HasValue)
            {
                int numS = 0;
                for (int i = 0; i < NTrainPoints; i++)
                {
                    if (TrainPoints.Labels[i] == 's')
                    {
                        numS += 1;
                    }
                }
                _TotalEntropy = Entropy(numS, NTrainPoints - numS);
            }
			return _TotalEntropy.Value;
		}

        private Tuple<double, double> FindBestSplit(int localDimIndex)
        {
            const int NSPLITS = 5;
            double totalEntropy = TotalEntropy();

            int globalDimIndex = this.TargetFeatures[localDimIndex];
            int[] globalDimIndices = Yarr.Repeat<int>(globalDimIndex, 1);

            double[] localMins = this.TrainPoints.CalcLocalMins(globalDimIndices);
            double[] localMaxs = this.TrainPoints.CalcLocalMaxs(globalDimIndices);

            double dimMin = localMins[0];
            double dimMax = localMaxs[0];

            double[] splits = RandomUtils.RandBetween(dimMin, dimMax, NSPLITS);

            double maxExpectedInfo = 0.0;
            double bestSplit = double.NaN;

            for (int i=0; i<NSPLITS; i++)
            {
                double split = splits[i];

                int nAbove, sAbove, bAbove;
                int nBelow, sBelow, bBelow;
                nAbove = sAbove = bAbove = nBelow = sBelow = bBelow = 0;
                for (int rowNum = 0; rowNum < NTrainPoints; rowNum++)
                {
                    double val = TrainPoints.FeatureCols[globalDimIndex][rowNum];
                    bool isSignal = TrainPoints.Labels[rowNum] == 's';

                    if (val >= split)
                    {
                        nAbove++;
                        if (isSignal) { sAbove++; } else { bAbove++; }
                    }
                    else
                    {
                        nBelow++;
                        if (isSignal) { sBelow++; } else { bBelow++; }
                    }
                }

                double probAbove = ((double)nAbove) / NTrainPoints;
                double probBelow = 1.0 - probAbove; // == ((double)nBelow) / NTrainPoints

                double entropyAbove = Entropy(sAbove, bAbove);
                double entropyBelow = Entropy(sBelow, bBelow);

                double expectedInfo = totalEntropy - ((probAbove * entropyAbove) + (probBelow * entropyBelow));

                if (expectedInfo > maxExpectedInfo)
                {
                    maxExpectedInfo = expectedInfo;
                    bestSplit = split;
                }
            }

            return new Tuple<double, double>(maxExpectedInfo, bestSplit);
        }

		private Tuple<int, double, double> FindBestSplit()
		{
            int bestLocalDimIndex = -1;
            double maxExpectedInfo = 0.0;
            double bestSplit = double.NaN;

			//for each dimension, test out NSPLITS
			//evently spaced splits, and pick the best
			//one out of all the dimensions
			for (int localDimIndex=0; localDimIndex<this.NDim; localDimIndex++)
			{
                var dimResult = FindBestSplit(localDimIndex);
                double expectedInfo = dimResult.Item1;
                double split = dimResult.Item2;

                if (expectedInfo > maxExpectedInfo)
                {
                    bestLocalDimIndex = localDimIndex;
                    bestSplit = split;
                    maxExpectedInfo = expectedInfo;
                }
			}

			return new Tuple<int, double, double>(bestLocalDimIndex, bestSplit, maxExpectedInfo);
		}

		private Tuple<int, double, double> FindBestRandomSplit()
		{
            int localDimIndex = RNG.Next(this.NDim);
            var result = FindBestSplit(localDimIndex);
            double expectedInfo = result.Item1;
            double split = result.Item2;
            return new Tuple<int, double, double>(localDimIndex, split, expectedInfo);
		}

		private List<Tree> Split()
		{
			var splitInfo = FindBestSplit();
			//var splitInfo = FindBestRandomSplit();
			int bestLocalDimIndex = splitInfo.Item1;
			double bestSplit = splitInfo.Item2;
			double maxExpectedInfo = splitInfo.Item3;

			if (bestLocalDimIndex == -1)
			{
				return null;
			}

			int bestGlobalDimIndex = this.TargetFeatures[bestLocalDimIndex];
			bool[] filter = TrainPoints.FeatureCols[bestGlobalDimIndex].Geq(bestSplit);

			var upperMinCorner = new double[NDim];
			MinCorner.CopyTo(upperMinCorner, 0);
			upperMinCorner[bestLocalDimIndex] = bestSplit;

			var lowerMaxCorner = new double[NDim];
			MaxCorner.CopyTo(lowerMaxCorner, 0);
			lowerMaxCorner[bestLocalDimIndex] = bestSplit;

			var upperTree = new Tree(
                TrainPoints.Filter(filter),
				this.TargetFeatures,
                upperMinCorner,
                MaxCorner,
				includeMax: IncludeMax,
				normalizingConstant: NormalizingConstant
            );

			bool[] lowerIncludeMax = new bool[NDim];
			IncludeMax.CopyTo(lowerIncludeMax, 0);
			lowerIncludeMax[bestLocalDimIndex] = false;

			Yarr.InlineNot(filter);
			var lowerTree = new Tree(
				TrainPoints.Filter(filter),
				this.TargetFeatures,
				MinCorner,
				lowerMaxCorner,
				includeMax: lowerIncludeMax,
				normalizingConstant: NormalizingConstant
			);

			this.SplitDim = bestLocalDimIndex;
			this.SplitVal = bestSplit;
			return new List<Tree> { upperTree, lowerTree };
		}

		public Score Score(RecordSet data, bool parallel=true)
		{
			//NOTE: ignore parallel parameter

			double[] sScores = Yarr.Repeat(double.NaN, data.NRows);
			double[] bScores = Yarr.Repeat(double.NaN, data.NRows);

			bool[] filter = Yarr.InlineNot(data.HasNaN(this.TargetFeatures));
			var filteredData = data.Filter(filter);
			data = null; // unlikely to let anything be GC'ed (lots of references to same obj) but it can't hurt

			this._Score(
				filteredData,
				Yarr.Range(filteredData.NRows).MakeSlice(),
				sScores,
				bScores
			);
			return new Score(sScores, bScores);
		}

		protected void _Score(RecordSet unfilteredData, Slice<int> returnIndex, double[] sScores, double[] bScores)
		{
			int nrows = returnIndex.Length;
			if (this.Children==null || !this.Children.Any())
			{
				double sDensity = this.SDensity;
				double bDensity = this.BDensity;
				for (int i=0; i<nrows; i++)
				{
					int index = returnIndex[i];
					sScores[index] = sDensity;
					bScores[index] = bDensity;
				}
			}
			else
			{
				var subSlices = ReorderSlice(
					returnIndex,
					unfilteredData.FeatureCols[this.TargetFeatures[this.SplitDim]],
					this.SplitVal
				);
				var upperSlice = subSlices.Item1;
				var lowerSlice = subSlices.Item2;

				var upperChild = this.Children[0];
				upperChild._Score(unfilteredData, upperSlice, sScores, bScores);

				var lowerChild = this.Children[1];
				lowerChild._Score(unfilteredData, lowerSlice, sScores, bScores);
			}
		}

		/// <summary>
		/// slice is assumed to be a slice of indexes into data on column dim.
		/// Reorder the slice inline so that all indices pointing to dim-values
		/// that are >= splitVal are at the front, and all others are at the back.
		/// Return two new slices corresponding to those regions (the &gt;= slice is
		/// first, the &lt; slice is second)
		/// </summary>
		public static Tuple<Slice<int>, Slice<int>> ReorderSlice(Slice<int> slice, Indexer<double> featureCol, double splitVal)
		{
			int swap;
			int startIndex = 0;
			int endIndex = slice.Length - 1;

			while(true)
			{
				while(featureCol[slice[startIndex]] >= splitVal)
				{
					startIndex++;
				}

				while(featureCol[slice[endIndex]] < splitVal)
				{
					endIndex--;
				}

				if (startIndex < endIndex)
				{
					swap = slice[startIndex];
					slice[startIndex] = slice[endIndex];
					slice[endIndex] = swap;
				}
				else
				{
					return new Tuple<Slice<int>, Slice<int>>(
						slice.MakeSlice(0, startIndex),
						slice.MakeSlice(startIndex, slice.Length - startIndex)
					);
				}
			}
		}
	}
}

