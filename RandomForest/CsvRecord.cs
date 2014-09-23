using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace RandomForest
{
    public class CsvRecord
    {
		public static List<string> FEATURE_COLS = getFeatureCols();
		public static readonly int NUM_FEATURES = FEATURE_COLS.Count;

		// private void SetCol(int col, double val) { this.DataStore[this.Index, col] = val; }
		// private void GetCol(int col) { return this.DataStore[this.Index, col]; }

		public int EventId { get; set; }
		public double DER_mass_MMC { get; set; }
		public double DER_mass_transverse_met_lep { get; set; }
		public double DER_mass_vis { get; set; }
		public double DER_pt_h { get; set; }
		public double DER_deltaeta_jet_jet { get; set; }
		public double DER_mass_jet_jet { get; set; }
		public double DER_prodeta_jet_jet { get; set; }
		public double DER_deltar_tau_lep { get; set; }
		public double DER_pt_tot { get; set; }
		public double DER_sum_pt { get; set; }
		public double DER_pt_ratio_lep_tau { get; set; }
		public double DER_met_phi_centrality { get; set; }
		public double DER_lep_eta_centrality { get; set; }
		public double PRI_tau_pt { get; set; }
		public double PRI_tau_eta { get; set; }
		public double PRI_tau_phi { get; set; }
		public double PRI_lep_pt { get; set; }
		public double PRI_lep_eta { get; set; }
		public double PRI_lep_phi { get; set; }
		public double PRI_met { get; set; }
		public double PRI_met_phi { get; set; }
		public double PRI_met_sumet { get; set; }
		public int PRI_jet_num { get; set; }
		public double PRI_jet_leading_pt { get; set; }
		public double PRI_jet_leading_eta { get; set; }
		public double PRI_jet_leading_phi { get; set; }
		public double PRI_jet_subleading_pt { get; set; }
		public double PRI_jet_subleading_eta { get; set; }
		public double PRI_jet_subleading_phi { get; set; }
		public double PRI_jet_all_pt { get; set; }

		private static List<string> getFeatureCols() {
			return typeof(CsvRecord)
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Select(fld => fld.Name)
				.Where(name => name.StartsWith("DER") || name.StartsWith("PRI"))
				.ToList();
		}
    }

	public class TrainingCsvRecord : CsvRecord
	{
		public double Weight { get; set; }
		public char Label { get; set; }
	}
}

