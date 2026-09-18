using System;
using System.Collections.Generic;
using System.Text;

namespace AudioUploader.Application.DTOs
{
    public record SummaryJobInput(Guid JobId, string Text);
}
