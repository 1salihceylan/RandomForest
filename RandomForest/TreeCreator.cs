using System;
using System.Collections.Generic;
using System.Linq;
using YarrLib;
using System.Threading.Tasks;

namespace RandomForest
{
	public class TreeCreator
	{
		private TrainingRecordSet Data;
		private int[] ColIndices;
		private int ColsPerTree;

		private double[] GlobalMinCorner;
		private double[] GlobalMaxCorner;

		public TreeCreator(TrainingRecordSet data, int[] colIndices, int colsPerTree)
		{
			this.Data = data;
			this.ColIndices = colIndices;
			this.ColsPerTree = colsPerTree;

			this.GlobalMinCorner = data.GlobalMins;
			this.GlobalMaxCorner = data.GlobalMaxs;
		}

		private Tree MakeTree()
		{
			var cols = RandomUtils.Choice(this.ColIndices, this.ColsPerTree).ToArray();

			bool[] filter = Yarr.InlineNot(this.Data.HasNaN(cols));
			var filteredData = this.Data.Filter(filter);

			var result = new Tree(
				filteredData,
				cols,
				Yarr.FancyIndex(this.GlobalMinCorner, cols),
				Yarr.FancyIndex(this.GlobalMaxCorner, cols)
			);

			result.Train();

			return result;
		}

		public List<Tree> MakeTrees(int ntrees)
		{
			double[] globalMinCorner = this.Data.GlobalMins;
			double[] globalMaxCorner = this.Data.GlobalMaxs;

			return Enumerable.Range(0, ntrees)
				.Select(index => this.MakeTree())
				.ToList();
		}

		public List<Tree> MakeTreesParallel(int ntrees)
		{
			int cores = Environment.ProcessorCount;

			int treesPerCore = ntrees / cores; // int division == floor
			int[] coreChunks = Yarr.Repeat(treesPerCore, cores);

			int diff = ntrees - (treesPerCore * cores);
			for (int i=0; i<diff; i++)
			{
				coreChunks[i]++;
			}

			Task<List<Tree>>[] tasks = new Task<List<Tree>>[cores];
			for (int i=0; i<cores; i++)
			{
				int privateI = i;
				tasks[i] = Task.Factory.StartNew(
					() => MakeTrees(coreChunks[privateI])
				);
			}
			Task.WaitAll(tasks);

			return tasks.SelectMany(t => t.Result).ToList();
		}

		public List<Tree> MakeTreesTimed(TimeSpan duration)
		{
			DateTime end = DateTime.Now.Add(duration);

			List<Tree> result = new List<Tree>();
			while(DateTime.Now < end)
			{
				result.Add(this.MakeTree());
			}
			return result;
		}

		public List<Tree> MakeTreesTimedParallel(TimeSpan duration)
		{
			int procs = Environment.ProcessorCount;
			Task<List<Tree>>[] tasks = new Task<List<Tree>>[procs];
			for (int i=0; i<procs; i++)
			{
				tasks[i] = Task.Factory.StartNew(
					() => MakeTreesTimed(duration)
				);
			}
			Task.WaitAll(tasks);

			return tasks.SelectMany(task => task.Result).ToList();
		}
	}
}

