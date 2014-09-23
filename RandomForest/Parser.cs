using System;
using CsvHelper;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RandomForest
{
    public static class Parser
    {
		public static TrainingRecordSet LoadTrainData()
		{
			return new TrainingRecordSet(
				LoadData<TrainingCsvRecord>("Resources/training.csv")
			);
        }

		public static RecordSet LoadTestData()
		{
			return new RecordSet(
				LoadData<CsvRecord>("Resources/test.csv")
			);
		}

		private static List<T> LoadData<T>(string filename)
			where T : CsvRecord
		{
			using (TextReader textReader = File.OpenText(filename))
			{
				var csvReader = new CsvReader(textReader);
				return csvReader.GetRecords<T>().ToList();
			}
		}

		public static void WritePredictions(Indexer<int> eventId, char[] predictions, int[] rankOrder)
		{
			using (TextWriter textWriter = File.CreateText("../output.csv"))
			{
				var csv = new CsvWriter(textWriter);
				csv.WriteRecords(ZipPredictions(eventId, predictions, rankOrder));
			}
		}

		private static IEnumerable<OutputCsvRecord> ZipPredictions(Indexer<int> eventId, char[] predictions, int[] rankOrder)
		{
			for (int i=0; i<eventId.Length; i++)
			{
				yield return new OutputCsvRecord
				{
					EventId = eventId[i],
					RankOrder = rankOrder[i],
					Class = predictions[i]
				};
			}
		}

		public class OutputCsvRecord
		{
			public int EventId { get; set; }
			public int RankOrder { get; set; }
			public char Class { get; set; }
		}
    }
}

