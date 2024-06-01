using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserControllerRecordingService.Model
{
    class ProcessUserControlRecordingRequest
    {
        public string LeadTransitId { get; set; }
        public string RecordingIntervals { get; set; }
        public string PhoneNumber { get; set; }
        public bool AddPauseAnnouncement { get; set; }
        public DateTime RecordingStartTime { get; set; }
        public string DestinationBucketPath { get; set; }
    }
}
