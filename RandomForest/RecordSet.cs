using System;
using System.Collections.Generic;
using System.Linq;
using YarrLib;

namespace RandomForest
{
	public class RecordSet
	{
		public readonly Indexer<double>[] FeatureCols;
		public readonly Indexer<int> EventIds;
		public int[] Index;

		private readonly double[][] _FeatureCols;
		private readonly int[] _EventIds;

		public int NRows { get; protected set; }
		public int NFeatures { get; protected set; }

		public RecordSet(List<CsvRecord> data, double nanValue=-999.0)
		{
			NRows = data.Count;
			NFeatures = CsvRecord.NUM_FEATURES;

			Index = new int[NRows];

			_EventIds = new int[NRows];
			EventIds = new Indexer<int>(this, _EventIds);

			_FeatureCols = new double[NFeatures][];
			FeatureCols = new Indexer<double>[NFeatures];
			for (int featureIndex=0; featureIndex<NFeatures; featureIndex++)
			{
				_FeatureCols[featureIndex] = new double[NRows];
				FeatureCols[featureIndex] = new Indexer<double>(this, _FeatureCols[featureIndex]);
			}


			#region fill arrays
			for (int rownum=0; rownum<NRows; rownum++)
			{
				var row = data[rownum];

				_EventIds[rownum] = row.EventId;
				Index[rownum] = rownum;

				_FeatureCols[0][rownum] = row.DER_mass_MMC;
				_FeatureCols[1][rownum] = row.DER_mass_transverse_met_lep;
				_FeatureCols[2][rownum] = row.DER_mass_vis;
				_FeatureCols[3][rownum] = row.DER_pt_h;
				_FeatureCols[4][rownum] = row.DER_deltaeta_jet_jet;
				_FeatureCols[5][rownum] = row.DER_mass_jet_jet;
				_FeatureCols[6][rownum] = row.DER_prodeta_jet_jet;
				_FeatureCols[7][rownum] = row.DER_deltar_tau_lep;
				_FeatureCols[8][rownum] = row.DER_pt_tot;
				_FeatureCols[9][rownum] = row.DER_sum_pt;
				_FeatureCols[10][rownum] = row.DER_pt_ratio_lep_tau;
				_FeatureCols[11][rownum] = row.DER_met_phi_centrality;
				_FeatureCols[12][rownum] = row.DER_lep_eta_centrality;
				_FeatureCols[13][rownum] = row.PRI_tau_pt;
				_FeatureCols[14][rownum] = row.PRI_tau_eta;
				_FeatureCols[15][rownum] = row.PRI_tau_phi;
				_FeatureCols[16][rownum] = row.PRI_lep_pt;
				_FeatureCols[17][rownum] = row.PRI_lep_eta;
				_FeatureCols[18][rownum] = row.PRI_lep_phi;
				_FeatureCols[19][rownum] = row.PRI_met;
				_FeatureCols[20][rownum] = row.PRI_met_phi;
				_FeatureCols[21][rownum] = row.PRI_met_sumet;
				_FeatureCols[22][rownum] = row.PRI_jet_num;
				_FeatureCols[23][rownum] = row.PRI_jet_leading_pt;
				_FeatureCols[24][rownum] = row.PRI_jet_leading_eta;
				_FeatureCols[25][rownum] = row.PRI_jet_leading_phi;
				_FeatureCols[26][rownum] = row.PRI_jet_subleading_pt;
				_FeatureCols[27][rownum] = row.PRI_jet_subleading_eta;
				_FeatureCols[28][rownum] = row.PRI_jet_subleading_phi;
				_FeatureCols[29][rownum] = row.PRI_jet_all_pt;
			}
			#endregion

			for (int featureNum=0; featureNum<NFeatures; featureNum++)
			{
				Yarr.InlineReplace(
					_FeatureCols[featureNum],
					nanValue,
					double.NaN
				);
			}

		}

		protected RecordSet(RecordSet copyFrom, bool[] filter)
		{
			this.Index = Yarr.Filter(copyFrom.Index, filter);
			this.NRows = this.Index.Length;
			this.NFeatures = copyFrom.NFeatures;

			this._EventIds = copyFrom._EventIds;
			this.EventIds = new Indexer<int>(this, this._EventIds);

			this._FeatureCols = copyFrom._FeatureCols;
			this.FeatureCols = new Indexer<double>[this.NFeatures];
			for (int featureNum=0; featureNum<this.NFeatures; featureNum++)
			{
				this.FeatureCols[featureNum] = new Indexer<double>(this, this._FeatureCols[featureNum]);
			}

			this._GlobalMins = copyFrom._GlobalMins;
			this._GlobalMaxs = copyFrom._GlobalMaxs;
		}

		public bool[] HasNaN(int[] cols)
		{
			bool[] filter = new bool[this.NRows];
			for(int row=0; row<this.NRows; row++)
			{
				bool isFiltered = false;
				for(int colNum=0; colNum<cols.Length; colNum++)
				{
					int col = cols[colNum];
					isFiltered |= double.IsNaN(this.FeatureCols[col][row]);
				}
				filter[row] = isFiltered;
			}
			return filter;
		}

		public bool[] HasNaN()
		{
			int nrows = this.NRows;
			int nfeatures = this.NFeatures;

			bool[] filter = new bool[nrows];
			for (int i=0; i<nrows; i++)
			{
				bool isFiltered = false;
				for (int f=0; f<nfeatures; f++)
				{
					isFiltered |= double.IsNaN(this.FeatureCols[f][i]);
				}
				filter[i] = isFiltered;
			}
			return filter;
		}

		public virtual RecordSet FilterRecordSet(bool[] filter)
		{
			return new RecordSet(this, filter);
		}

		private double[] CalcGlobalMins()
		{
			double[] mins = new double[this.NFeatures];
			for (int featureIndex=0; featureIndex<this.NFeatures; featureIndex++)
			{
				mins[featureIndex] = Yarr.Min(this._FeatureCols[featureIndex]);
			}
			return mins;
		}

		private double[] CalcGlobalMaxs()
		{
			double[] maxs = new double[this.NFeatures];
			for (int i=0; i<this.NFeatures; i++)
			{
				maxs[i] = Yarr.Max(this._FeatureCols[i]);
			}
			return maxs;
		}

		private double[] _GlobalMins;
		public double[] GlobalMins
		{
			get
			{
				if (_GlobalMins==null)
				{
					_GlobalMins = CalcGlobalMins();
				}
				return _GlobalMins;
			}
		}

		private double[] _GlobalMaxs;
		public double[] GlobalMaxs
		{
			get
			{
				if (_GlobalMaxs == null)
				{
					_GlobalMaxs = CalcGlobalMaxs();
				}
				return _GlobalMaxs;
			}
		}

		public double[] CalcLocalMins()
		{
			double[] mins = new double[this.NFeatures];
			for (int featureIndex=0; featureIndex<this.NFeatures; featureIndex++)
			{
				mins[featureIndex] = this.FeatureCols[featureIndex].Min();
			}
			return mins;
		}

		public double[] CalcLocalMaxs()
		{
			double[] maxs = new double[this.NFeatures];
			for (int i=0; i<this.NFeatures; i++)
			{
				maxs[i] = this.FeatureCols[i].Max();
			}
			return maxs;
		}

		public double[] CalcLocalMins(int[] dims)
		{
			int nrows = this.NRows;
			int nfeatures = dims.Length;

			double[] mins = new double[nfeatures];
			for (int dimIndex=0; dimIndex<nfeatures; dimIndex++)
			{
				int dim = dims[dimIndex];
				mins[dimIndex] = this.FeatureCols[dim].Min();
			}
			return mins;
		}

		public double[] CalcLocalMaxs(int[] dims)
		{
			int nrows = this.NRows;
			int nfeatures = dims.Length;

			double[] maxs = new double[nfeatures];
			for (int dimIndex=0; dimIndex<nfeatures; dimIndex++)
			{
				int dim = dims[dimIndex];
				maxs[dimIndex] = this.FeatureCols[dim].Max();
			}
			return maxs;
		}
	}

	public class TrainingRecordSet : RecordSet
	{
		public Indexer<double> Weights;
		public Indexer<char> Labels;

		protected double[] _Weights;
		protected char[] _Labels;

		public TrainingRecordSet(List<TrainingCsvRecord> data): base(data.Cast<CsvRecord>().ToList())
		{
			_Weights = new double[NRows];
			Weights = new Indexer<double>(this, _Weights);

			_Labels = new char[NRows];
			Labels = new Indexer<char>(this, _Labels);

			for (int i=0; i<NRows; i++)
			{
				var row = data[i];

				_Weights[i] = row.Weight;
				_Labels[i] = row.Label;
			}
		}

		protected TrainingRecordSet(TrainingRecordSet copyFrom, bool[] filter)
			: base(copyFrom, filter)
		{
			this._Weights = copyFrom._Weights;
			this.Weights = new Indexer<double>(this, this._Weights);

			this._Labels = copyFrom._Labels;
			this.Labels = new Indexer<char>(this, this._Labels);
		}

		public override RecordSet FilterRecordSet(bool[] filter)
		{
			return new TrainingRecordSet(this, filter);
		}
	}

	public static class TrainSetExtensions
	{
		public static T Filter<T>(this T self, bool[] filter)
			where T : RecordSet
		{
			return (T)self.FilterRecordSet(filter);
		}
	}
}

