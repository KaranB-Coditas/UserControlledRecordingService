using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using Microsoft.VisualBasic.FileIO;
using UserControllerRecordingService.Model;

namespace UserControllerRecordingService
{
    class Program
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            Console.WriteLine("Enter '1' to Start Job");
            var input = Console.ReadLine();

            if (input == "1")
            {
                StartProcessRecording();
            }

            Console.ReadLine();
        }

        public static void StartProcessRecording()
        {
            Logger.Info("Started processing recording");
            Console.WriteLine("Started processing recording");

            var csvHelper = new CsvHelper();
            var processRecording = new ProcessRecording();

            string csvFilePath = "data.csv";
            var records = csvHelper.ReadCsvFile(csvFilePath);

            if (records.Any())
            {
                var unprocessedData = processRecording.Process(records);

                if (unprocessedData?.Count > 0)
                {
                    Logger.Info("Some recording file not processed");
                    csvHelper.WriteCSVFile("failed.csv", unprocessedData);
                }
                else
                {
                    Logger.Info("Successfully all recording processed and trimmed!!!");
                    Console.WriteLine("Successfully all recording processed and trimmed!!!");
                }

            }
            else
            {
                Logger.Info("No data to process");
                Console.WriteLine("No data to process");
            }

            Logger.Info("End of processing recording");
            Console.WriteLine("End of processing recording");
        }
        
    }
}
