using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserControllerRecordingService.Model
{
    class ResponseResult
    {
        public bool FetchRecordingFromCdrToGcs { get; set; }
        public bool TrimUserControlledRecording { get; set; }
        public CsvRecord Record { get; set; }
    }
}
