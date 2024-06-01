using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserControllerRecordingService.Model
{
    public class CsvRecord
    {
        public string LeadTransitId { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime RecordingStartTime { get; set; }
        public int AgentCallTransferredTimeDifference { get; set; }
        public string RecordingIntervals { get; set; }
    }
}
