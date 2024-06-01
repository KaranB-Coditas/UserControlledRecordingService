using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UserControllerRecordingService.Model;
using UserControllerRecordingService.Constants;


namespace UserControllerRecordingService
{
    class ProcessRecording
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void ProcessTask(List<CsvRecord> records)
        {
            var taskList = new List<Task<HttpResponseMessage>>();
            foreach (var record in records)
            {
                string correlationId = Guid.NewGuid().ToString();
                var task = Task.Run(() =>
                {
                    return FetchRecordingFromCdrToGcs(record, "", "", correlationId);
                });
                taskList.Add(task);
            }
            Task.WhenAll(taskList).GetAwaiter().GetResult();

            // Print the results
            foreach (var task in taskList)
            {
                Logger.Info(task.Result);
            }
        }
        public List<ResponseResult> Process(List<CsvRecord> records)
        {
            var unprocessedRecording = new List<ResponseResult>();

            foreach (var record in records)
            {
                string correlationId = Guid.NewGuid().ToString();
                var responseResult = new ResponseResult();
                var destinationPath = GetDestinationPath(record.RecordingStartTime, Constants.Environment.INTUIT);

                var fetchRecordingResponse = FetchRecordingFromCdrToGcs(record, destinationPath, null, correlationId);

                if (fetchRecordingResponse.IsSuccessStatusCode)
                {
                    var trimRecordingResponse = new HttpResponseMessage();
                    var recordingInterval = GetRecordingInterval(record.LeadTransitId, record.RecordingIntervals, record.AgentCallTransferredTimeDifference);
                    
                    if(recordingInterval != null)
                    {
                        record.RecordingIntervals = recordingInterval;
                        trimRecordingResponse = TrimUserControlledRecording(record, destinationPath, false, correlationId);
                    }
                        
                    if (record.RecordingIntervals == null || !trimRecordingResponse.IsSuccessStatusCode)
                    {
                        responseResult.Record = record;
                        responseResult.FetchRecordingFromCdrToGcs = true;
                        responseResult.TrimUserControlledRecording = false;
                        unprocessedRecording.Add(responseResult);
                    }
                }
                else
                {
                    responseResult.Record = record;
                    responseResult.FetchRecordingFromCdrToGcs = false;
                    responseResult.TrimUserControlledRecording = false;
                    unprocessedRecording.Add(responseResult);
                }
            }
            return unprocessedRecording;
        }

        private string GetDestinationPath(DateTime datetime, string environment)
        {
            var localStartTime = TimeZoneInfo.ConvertTimeFromUtc(datetime, TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"));
            return $"cas-mp3-recording/{environment}/Recordings/{localStartTime.Date.ToString("yyyy/M/d")}";
        }

        private string GetRecordingInterval(string leadTransitId, string recordingInterval, int transferredTimeDiff)
        {
            try
            {
                Logger.Info($"Intervals for leadtransitId: {leadTransitId} = {recordingInterval}");
                var recordingIntervalsList = JsonConvert.DeserializeObject<List<RecordingIntervalModel>>(recordingInterval);
                var filteredIntervals = recordingIntervalsList.Where(interval => interval.RecordStartTime != interval.RecordStopTime).ToList();

                if (filteredIntervals != null && filteredIntervals.Any() && transferredTimeDiff >= 0)
                {
                    var distinctIntervals = filteredIntervals
                       .GroupBy(interval => new { interval.RecordStartTime, interval.RecordStopTime })
                       .Select(group => group.FirstOrDefault())
                       .ToList();

                    if (distinctIntervals != null && distinctIntervals.Any())
                    {
                        Logger.Info($"User control Transferred Time Difference for leadtransitId: {leadTransitId} = {transferredTimeDiff}");

                        distinctIntervals.ForEach(x => x.RecordStartTime += transferredTimeDiff);
                        distinctIntervals.ForEach(x => x.RecordStopTime += transferredTimeDiff);

                        string intervalStr = JsonConvert.SerializeObject(distinctIntervals);
                        Logger.Info($"User control request after filtering for leadtransitId: {leadTransitId} = {intervalStr}");
                        return intervalStr;
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                Logger.Error($"Error occured while getting recording interval {e}");
                return null;
            }
            
        }

        private HttpResponseMessage FetchRecordingFromCdrToGcs(CsvRecord postRequest,string destinationBucketpath, string crmType, string correlationId)
        {
            try
            {
                Logger.Info($"Sending Http call to recording API in SendHttpRequestPostAsync method for correlationId : {correlationId}");

                var url = "https://recording-api.connectandsell.com/api/v1/recordings/persistent";
                HttpResponseMessage response = new HttpResponseMessage(System.Net.HttpStatusCode.NoContent);

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders
                       .Accept
                           .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("secretToken", "testRecording");
                    client.DefaultRequestHeaders.Add("correlationId", correlationId);

                    var request = new ProcessRecordingRequest()
                    {
                        LeadTransitId = postRequest.LeadTransitId,
                        PhoneNumber = postRequest.PhoneNumber,
                        RecordingStartTime = postRequest.RecordingStartTime,
                        DestinationBucketPath = destinationBucketpath,
                        CRMType = crmType
                    };

                    var jsonRequest = JsonConvert.SerializeObject(request);

                    Logger.Info($"Http Recording API Url:{url}  for correlationId : {correlationId}");
                    Logger.Info($"Http PostAsync call to execute Recording API with request: {jsonRequest}  for correlationId : {correlationId}");

                    response = client.PostAsync(url, new StringContent(jsonRequest, Encoding.UTF8, "application/json")).Result;
                    Logger.Info($"Http Response : {response?.ToString()}  for correlationId : {correlationId}");

                    var content = response?.Content?.ReadAsStringAsync();
                    Logger.Info($"Recording API StatusCode : {response.StatusCode} for correlationId : {correlationId}");
                    Logger.Info($"Recording API Response : {content} for correlationId : {correlationId}");
                }
                return response;
            }
            catch (Exception e)
            {
                Logger.Info($"Exception occured during post data for leadtransit id: {postRequest.LeadTransitId}, Error: {e} for correlationId : {correlationId}");
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            }
        }

        public HttpResponseMessage TrimUserControlledRecording(CsvRecord postRequest, string destinationBucketpath, bool addPauseAnnouncement, string correlationId)
        {
            try
            {
                Logger.Info($"Sending Http call to user controlled recording API in SendHttpRequestPostAsync method for correlationId : {correlationId}");
                HttpResponseMessage response = new HttpResponseMessage(System.Net.HttpStatusCode.NoContent);
                var url = "https://recording-api.connectandsell.com/api/v1/recordings/intervals";

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders
                       .Accept
                           .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var request = new ProcessUserControlRecordingRequest()
                    {
                        LeadTransitId = postRequest.LeadTransitId,
                        RecordingIntervals = postRequest.RecordingIntervals,
                        PhoneNumber = postRequest.PhoneNumber,
                        AddPauseAnnouncement = addPauseAnnouncement,
                        RecordingStartTime = postRequest.RecordingStartTime,
                        DestinationBucketPath = destinationBucketpath
                    };
                    Logger.Info($"request object: {request} for correlationId : {correlationId}");
                    var jsonRequest = JsonConvert.SerializeObject(request);
                    Logger.Info($"Http User Control Recording API Url: {url} for correlationId : {correlationId}");
                    Logger.Info($"Http PostAsync call to execute User Control Recording API with request: {jsonRequest} for correlationId : {correlationId}");

                    response = client.PostAsync(url, new StringContent(jsonRequest, Encoding.UTF8, "application/json")).Result;
                    Logger.Info($"Http Response : {response?.ToString()} for correlationId : {correlationId}");
                    var content = response?.Content?.ReadAsStringAsync();
                    Logger.Info($"User Control Recording API StatusCode : {response.StatusCode} for correlationId : {correlationId}");
                    Logger.Info($"User Control Recording API Response : {content} for correlationId : {correlationId}");
                }
                return response;
            }
            catch (Exception e)
            {

                Logger.Info($"Exception occured during post data to trim recording for leadtransit id: {postRequest.LeadTransitId}, Error: {e} for correlationId : {correlationId}");
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            }
        }
    }
}
