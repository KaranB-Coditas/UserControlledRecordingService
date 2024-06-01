using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Globalization;
using UserControllerRecordingService.Model;
using log4net;
using System.IO;

namespace UserControllerRecordingService
{
    class CsvHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public  List<CsvRecord> ReadCsvFile(string filePath)
        {
            try
            {
                var records = new List<CsvRecord>();

                using (TextFieldParser parser = new TextFieldParser(filePath))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");

                    string[] headers = parser.ReadFields();

                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();

                        var record = new CsvRecord
                        {
                            LeadTransitId = fields[0],
                            PhoneNumber = fields[1],
                            RecordingStartTime = DateTime.ParseExact(fields[2], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                            AgentCallTransferredTimeDifference = int.Parse(fields[3]),
                            RecordingIntervals = fields[4]
                        };

                        records.Add(record);
                    }
                }

                return records;
            }
            catch (Exception e)
            {
                Logger.Error($"Error while reading csv file {e}");
                return null;
            }
            
        }

        public void WriteCSVFile(string filePath, List<ResponseResult> csvRecord)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    writer.Write("LeadTransitId,");
                    writer.Write("PhoneNumber,");
                    writer.Write("RecordingStartTime,");
                    writer.Write("AgentCallTransferredTimeDifference,");
                    writer.Write("RecordingIntervals,");
                    writer.Write("FetchRecordingFromCdrToGcs,");
                    writer.Write("TrimUserControlledRecording");
                    writer.WriteLine();
                    foreach (var record in csvRecord)
                    {
                        writer.Write(record.Record.LeadTransitId + ",");
                        writer.Write(record.Record.PhoneNumber + ",");
                        writer.Write(record.Record.RecordingStartTime + ",");
                        writer.Write(record.Record.AgentCallTransferredTimeDifference + ",");
                        writer.Write("\""+record.Record.RecordingIntervals + "\",");
                        writer.Write(record.FetchRecordingFromCdrToGcs + ",");
                        writer.Write(record.TrimUserControlledRecording + "");
                        writer.WriteLine();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error while creating csv file {e}");
            }

        }
    }
}
