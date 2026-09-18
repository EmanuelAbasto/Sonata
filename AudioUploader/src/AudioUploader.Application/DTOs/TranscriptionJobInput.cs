using System;
using System.Collections.Generic;
using System.Text;

namespace AudioUploader.Application.DTOs
{
    public record TranscriptionJobInput(Guid JobId, string FilePath);
}
