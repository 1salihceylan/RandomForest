using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using YarrLib;
using System.Media;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RandomForest
{
    static class MainClass
    {
		private const int NUM_MODELS = 1000;
		private const int COLS_PER_MODEL = 10;
		private const bool PARALLEL = true;

        public static void Main(string[] args)
        {
			MainMain();
        }

		private static void MainMain()
		{
			Write("Running Random Forest ({0} trees)", NUM_MODELS);

			Write("loading training data");
			var traindata = Parser.LoadTrainData();
			var featureCols = CsvRecord.FEATURE_COLS;
			int[] colIndices = Yarr.Range(featureCols.Count);
			WriteDone();

			Write("creating random forest");
			var treeCreator = new TreeCreator(traindata, colIndices, COLS_PER_MODEL);
			var trees = treeCreator.MakeTreesParallel(NUM_MODELS);
			ScoreAverager<Tree> forest = new ScoreAverager<Tree>(trees);
			WriteDone();
			Console.WriteLine(string.Format("\t\tcreated {0} trees", trees.Count));

			Write("creating and tuning classifier (parallel2)");
			PlayDingSound();
			double bestCutoff = double.NaN;
			double bestExponent = double.NaN;
			double bestScore = double.NegativeInfinity;
			var classifier = new Classifier(new ScoreCacher(forest));
			foreach(double exponent in Yarr.XRange(-0.5, 0.6, 0.1))
			{
				double cutoff = Math.Exp(exponent);
				classifier.Cutoff = cutoff;

				double score = AMS(classifier.Classify(traindata, parallel:PARALLEL), traindata);

				if (score > bestScore)
				{
					bestScore = score;
					bestCutoff = cutoff;
					bestExponent = exponent;
				}
			}
			classifier = new Classifier(forest);
			classifier.Cutoff = bestCutoff;
			WriteDone();
			Console.WriteLine(string.Format("\t\tpredicted ams: {0}", bestScore));
			Console.WriteLine(string.Format("\t\tcutoff: {0} (e^{1})", bestCutoff, bestExponent));

			if (bestScore < 3.5)
			{
				WriteDone();
				PlayFailSound();
				return;
			}

			Write("loading test data");
			var testdata = Parser.LoadTestData();
			WriteDone();

			Write("scoring test data");
			var predictions = classifier.Classify(testdata, parallel: PARALLEL);
			var confidences = Yarr.Range(1, testdata.NRows+1);
			WriteDone();

			Write("writing output");
			Parser.WritePredictions(testdata.EventIds, predictions, confidences);
			WriteDone();

			WriteDone(); //whole-method timer
			PlayWinSound();
		}

		private static void PlayWinSound()
		{
			PlaySound("ff7_win.mp3");
		}

		private static void PlayFailSound()
		{
			PlaySound("sadTrombone.mp3");
		}

		private static void PlayDingSound()
		{
			PlaySound("ding.mp3");
		}

		private static void PlaySound(string filename)
		{
			/* Process.Start(
				new ProcessStartInfo(
					"afplay",
					string.Format("Resources/{0}", filename)
				)
			); */
		}

		private const double TOTAL_S = 691.0;
		private const double TOTAL_B = 410000.0;
		private static double AMS(char[] predictions, TrainingRecordSet actual)
		{
			bool[] predictedSignal = Yarr.Equ(predictions, 's');
			bool[] actualSignal = actual.Labels.Equ('s');
			bool[] actualBackground = Yarr.Not(actualSignal);

			double total_s = actual.Filter(actualSignal).Weights.Sum();
			double total_b = actual.Filter(actualBackground).Weights.Sum();

			double[] scaledWeights = new double[predictions.Length];
			for (int i=0; i<predictions.Length; i++)
			{
				if (actualSignal[i])
				{
					scaledWeights[i] = actual.Weights[i] * (TOTAL_S / total_s);
				}
				else
				{
					scaledWeights[i] = actual.Weights[i] * (TOTAL_B / total_b);
				}
			}

			bool[] truePositives = Yarr.And(predictedSignal, actualSignal);
			bool[] falsePositives = Yarr.And(predictedSignal, actualBackground);

			double s = actual.Filter(truePositives).Weights.Sum();
			double b = actual.Filter(falsePositives).Weights.Sum();
			const double B_R = 10.0;

			double radicand = 2.0 * ((s + b + B_R) * Math.Log(1.0 + (s / (b + B_R))) - s);

			if (radicand < 0.0)
			{
				throw new Exception("radicand < 0.0, aborting");
			}
			else
			{
				return Math.Sqrt(radicand);
			}

		}

		private static bool HasDuplicates(List<string> lst)
		{
			return (new HashSet<string>(lst)).Count < lst.Count;
		}

		#region write helpers
		private static Stack<DateTime> writeTimerStack = new Stack<DateTime>();
		private static bool lastCallWasWrite = false;

		private static void Write(string msg, params object[] formatParams) {

			msg = string.Format(msg, formatParams);

			string prefix = "";
			if (writeTimerStack.Count > 0) {
				if (lastCallWasWrite) {
					prefix = "\n";
				}
				prefix += new String('\t', writeTimerStack.Count);
			}
				
			Console.Write(prefix + msg + "...");
			writeTimerStack.Push(DateTime.Now);
			lastCallWasWrite = true;
		}

		private static void WriteDone() {
			var elapsed = DateTime.Now - writeTimerStack.Pop();

			var pattern = lastCallWasWrite
				? " (Done - {0})"
				: (new String('\t', writeTimerStack.Count)) + "Done - {0}";

			Console.WriteLine(pattern, fmtTimeSpan(elapsed));
			lastCallWasWrite = false;
		}

		private static string fmtTimeSpan(TimeSpan ts) {
			string pattern;
			string suffix;
			if (ts.TotalHours >= 1.0) {
				pattern = @"%h\:%mm\:%ss";
				suffix = "hours";
			}
			else if(ts.TotalMinutes >= 1.0) {
				pattern = @"%m\:%ss";
				suffix = "mins";
			}
			else {
				pattern = @"%s";
				suffix = "secs";
			}

			return ts.ToString(pattern) + " " + suffix;
		}
		#endregion
    }
}
