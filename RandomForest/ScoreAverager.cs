using System;
using System.Collections.Generic;
using YarrLib;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;

namespace RandomForest
{
	public class ScoreAverager<T> : IScorer
		where T : IScorer
	{
		protected List<T> SubModels;

		public ScoreAverager(List<T> models)
		{
			this.SubModels = models;
		}

		public Score Score(RecordSet data, bool parallel=true)
		{
			if (parallel)
			{
				return ParallelGmeaner(
					ParallelScorer(data),
					data.NRows
				);
			}
			else
			{
				return GMean(
					this.SubModels.Select(model => model.Score(data, parallel: false)),
					data.NRows
				);
			}
		}

		private BlockingCollection<Score> ParallelScorer(RecordSet data)
		{
			int cores = Environment.ProcessorCount;


			var models = new BlockingCollection<T>(this.SubModels.Count);
			foreach(T model in this.SubModels)
			{
				models.Add(model);
			}
			models.CompleteAdding();
			Console.WriteLine("Done adding models");

			var scores = new BlockingCollection<RandomForest.Score>(cores * 10);
			int numScorerTasks = cores;
			Task[] scorerTasks = new Task[numScorerTasks];
			for (int i=0; i<numScorerTasks; i++)
			{
				scorerTasks[i] = Task.Factory.StartNew(
					() =>
					{
						T model;
						while (!models.IsCompleted)
						{
							try
							{
								model = models.Take();
							}
							catch (InvalidOperationException)
							{
								continue;
							}
							scores.Add(model.Score(data, parallel: false));
						}
					}
				);
			}

			//asynchronously wait for the scorer tasks to finish
			Task.Factory.StartNew(
				() =>
				{
					Console.WriteLine("Waiting for all scorer tasks to finish");
					Task.WaitAll(scorerTasks);
					Console.WriteLine("All score tasks finished");
					scores.CompleteAdding();
				}
			);

			return scores;
		}

		private Score ParallelGmeaner(BlockingCollection<Score> scores, int nrows)
		{
			double[] sSums = Yarr.Repeat<double>(0.0, nrows);
			double[] bSums = Yarr.Repeat<double>(0.0, nrows);
			int[] sCounts = Yarr.Repeat<int>(0, nrows);
			int[] bCounts = Yarr.Repeat<int>(0, nrows);

			int cores = Environment.ProcessorCount;
			BlockingCollection<double[]> sScoresCollection = new BlockingCollection<double[]>(cores * 10);
			BlockingCollection<double[]> bScoresCollection = new BlockingCollection<double[]>(cores * 10);

			Func<int[], double[], BlockingCollection<double[]>, Task> taskMaker =
				(counts, sums, scoreCollection) => Task.Factory.StartNew(
					() =>
					{
						double[] scoreArr;
						while(!scoreCollection.IsCompleted)
						{
							try
							{
								scoreArr = scoreCollection.Take();
							}
							catch (InvalidOperationException)
							{
								continue;
							}

							for (int rowIndex=0; rowIndex<nrows; rowIndex++)
							{
								double val = scoreArr[rowIndex];
								if (!double.IsNaN(val))
								{
									sums[rowIndex] += Math.Log(val);
									counts[rowIndex]++;
								}
							}
						}
					}
				);

			Task sTask = taskMaker(sCounts, sSums, sScoresCollection);
			Task bTask = taskMaker(bCounts, bSums, bScoresCollection);

			Score score;
			while(!scores.IsCompleted)
			{
				try
				{
					score = scores.Take();
				}
				catch (InvalidOperationException)
				{
					continue;
				}

				sScoresCollection.Add(score.SScores);
				bScoresCollection.Add(score.BScores);
			}

			Console.WriteLine("Done collecting sub-scores");
			sScoresCollection.CompleteAdding();
			bScoresCollection.CompleteAdding();
			Task.WaitAll(sTask, bTask);
			Console.WriteLine("All sub-score accumulators finished");

			double[] sScores = new double[nrows];
			double[] bScores = new double[nrows];
			for (int rowIndex=0; rowIndex<nrows; rowIndex++)
			{
				sScores[rowIndex] = Math.Exp(sSums[rowIndex] / sCounts[rowIndex]);
				bScores[rowIndex] = Math.Exp(bSums[rowIndex] / bCounts[rowIndex]);
			}

			return new Score(sScores, bScores);
		}

//		private Score ScoreParallel(RecordSet data)
//		{
//
//		}

		private Score GMean(IEnumerable<Score> scores, int nrows)
		{
			double[] sSums = Yarr.Repeat<double>(0.0, nrows);
			double[] bSums = Yarr.Repeat<double>(0.0, nrows);
			int[] sCounts = Yarr.Repeat<int>(0, nrows);
			int[] bCounts = Yarr.Repeat<int>(0, nrows);

			foreach(Score score in scores)
			{
				for (int rowIndex=0; rowIndex<nrows; rowIndex++)
				{
					double sScore = score.SScores[rowIndex];
					if (!double.IsNaN(sScore))
					{
						sSums[rowIndex] += Math.Log(sScore);
						sCounts[rowIndex]++;
					}

					double bScore = score.BScores[rowIndex];
					if (!double.IsNaN(bScore))
					{
						bSums[rowIndex] += Math.Log(bScore);
						bCounts[rowIndex]++;
					}
				}
			}

			double[] sScores = new double[nrows];
			double[] bScores = new double[nrows];
			for (int rowIndex=0; rowIndex<nrows; rowIndex++)
			{
				sScores[rowIndex] = Math.Exp(sSums[rowIndex] / sCounts[rowIndex]);
				bScores[rowIndex] = Math.Exp(bSums[rowIndex] / bCounts[rowIndex]);
			}

			return new Score(sScores, bScores);
		}

//		private Score ScoreNonParallel(RecordSet data)
//		{
//			return GMean(
//				this.SubModels.Select(model => model.Score(data)),
//				data.NRows
//			);
//		}

//		private Score ScoreNonParallel(RecordSet data)
//		{
//			//Compute the geometeric mean of the s-scores and the b-scores
//			//The geometric mean can be computed as either the
//			//nth root of the product of the inputs, or the exponent of the
//			//arithmetic mean of the logarithms.
//
//			int nrows = data.NRows;
//
//			double[] sSums = Yarr.Repeat<double>(0.0, nrows);
//			double[] bSums = Yarr.Repeat<double>(0.0, nrows);
//			int[] sCounts = Yarr.Repeat<int>(0, nrows);
//			int[] bCounts = Yarr.Repeat<int>(0, nrows);
//
//			foreach(T model in this.SubModels)
//			{
//				Score score = model.Score(data);
//
//				for (int rowIndex=0; rowIndex<nrows; rowIndex++)
//				{
//					double sScore = score.SScores[rowIndex];
//					if (!double.IsNaN(sScore))
//					{
//						sSums[rowIndex] += Math.Log(sScore);
//						sCounts[rowIndex]++;
//					}
//
//					double bScore = score.BScores[rowIndex];
//					if (!double.IsNaN(bScore))
//					{
//						bSums[rowIndex] += Math.Log(bScore);
//						bCounts[rowIndex]++;
//					}
//				}
//			}
//
//			double[] sScores = new double[nrows];
//			double[] bScores = new double[nrows];
//			for (int rowIndex=0; rowIndex<nrows; rowIndex++)
//			{
//				sScores[rowIndex] = Math.Exp(sSums[rowIndex] / sCounts[rowIndex]);
//				bScores[rowIndex] = Math.Exp(bSums[rowIndex] / bCounts[rowIndex]);
//			}
//
//			return new Score(sScores, bScores);
//		}
	}
}

