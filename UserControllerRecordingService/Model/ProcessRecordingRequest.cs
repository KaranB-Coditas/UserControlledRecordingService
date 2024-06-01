using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserControllerRecordingService.Model
{
    class ProcessRecordingRequest
    {
        public string LeadTransitId { get; set; }
        public string PhoneNumber { get; set; }
        public string DestinationBucketPath { get; set; }
        public DateTime RecordingStartTime { get; set; }
        public string CRMType { get; set; }
    }
}
