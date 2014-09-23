using System;

namespace RandomForest
{
	public class Indexer<T>
	{
		protected RecordSet Parent;
		public T[] Target;

		public Indexer(RecordSet parent, T[] target)
		{
			Parent = parent;
			Target = target;
		}

		protected Indexer() { }

		public int Length { get { return Parent.Index.Length; } }

		public virtual T this[int index]
		{
			get
			{
				return Target[Parent.Index[index]];
			}
		}

		public T[] ToArray()
		{
			T[] proxy = new T[this.Length];
			for (int i=0; i<this.Length; i++)
			{
				proxy[i] = this[i];
			}
			return proxy;
		}
	}

	public class ArrayIndexer<T> : Indexer<T>
	{
		protected int[] Index;

		public ArrayIndexer(int[] index, T[] target)
		{
			this.Index = index;
			this.Target = target;
		}

		public override T this[int index]
		{
			get { return this.Target[this.Index[index]]; }
		}
	}

	public static class IndexerExtensions
	{
		public static bool[] Gtr(this Indexer<double> self, double a)
		{
			int size = self.Length;
			var result = new bool[size];
			for (int i=0; i<size; i++)
			{
				result[i] = self[i] > a;
			}
			return result;
		}
		public static bool[] Geq(this Indexer<double> self, double a)
		{
			int size = self.Length;
			var result = new bool[size];
			for (int i=0; i<size; i++)
			{
				result[i] = self[i] >= a;
			}
			return result;
		}
		public static bool[] Equ(this Indexer<char> self, char a)
		{
			int size = self.Length;
			var result = new bool[size];
			for (int i=0; i<size; i++)
			{
				result[i] = self[i] == a;
			}
			return result;
		}
		public static int CountEqu(this Indexer<char> self, char c)
		{
			int count = 0;
			int size = self.Length;
			for (int i=0; i<size; i++)
			{
				if (self[i] == c)
				{
					count++;
				}
			}
			return count;
		}
		public static double Min(this Indexer<double> self)
		{
			double min = double.PositiveInfinity;
			int size = self.Length;
			double val;
			for (int i=0; i<size; i++)
			{
				val = self[i];
				if (val < min)
				{
					min = val;
				}
			}
			return min;
		}
		public static double Max(this Indexer<double> self)
		{
			double max = double.NegativeInfinity;
			int size = self.Length;
			double val;
			for (int i=0; i<size; i++)
			{
				val = self[i];
				if (val > max)
				{
					max = val;
				}
			}
			return max;
		}
		public static double Sum(this Indexer<double> self)
		{
			double sum = 0.0;
			int size = self.Length;
			for (int i=0; i<size; i++)
			{
				sum += self[i];
			}
			return sum;
		}
	}
}

