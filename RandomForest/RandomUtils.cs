using System;
using System.Collections.Generic;
using System.Linq;

namespace RandomForest
{
	public static class RandomUtils
	{
		public static Random Rng = new Random();

		private class IndexList : List<int>
		{
			public void InsertSorted(int index)
			{
				for (int i=0; i<this.Count; i++)
				{
					if (this[i] > index)
					{
						this.Insert(i, index);
						return;
					}
				}
				this.Add(index);
			}
		}

		public static IEnumerable<T> Choice<T>(List<T> source, int k)
		{
			return Choice(source.ToArray(), k);
		}

		public static IEnumerable<T> Choice<T>(T[] source, int k)
		{
			IndexList pastIndices = new IndexList();
			for (int i=0; i<k; i++) {
				int nextIndex = Rng.Next(source.Length - i);
				for (int j=0; j<i; j++)
				{
					if (nextIndex >= pastIndices[j])
					{
						nextIndex++;
					}
				}
				pastIndices.InsertSorted(nextIndex);
				yield return source[nextIndex];
			}
		}

		public static T Choice<T>(T[] source)
		{
			int index = Rng.Next(source.Length);
			return source[index];
		}

		public static double RandBetween(double low, double high)
		{
			double rangeSize = high - low;
			return (Rng.NextDouble() * rangeSize) + low;
		}

		public static double[] RandBetween(double low, double high, int howMany)
		{
			double[] result = new double[howMany];
			double rangeSize = high - low;
			for (int i=0; i<howMany; i++)
			{
				result[i] = (Rng.NextDouble() * rangeSize) + low;
			}
			return result;
		}
	}
}

