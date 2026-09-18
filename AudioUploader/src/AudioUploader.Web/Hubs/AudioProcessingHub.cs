using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace AudioUploader.Web.Hubs
{
    public class AudioProcessingHub : Hub
    {
        public async Task JoinJobGroup(string jobId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(jobId));
        }

        public async Task LeaveJobGroup(string jobId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(jobId));
        }

        public static string GroupName(string jobId) => $"job:{jobId}";
        public static string GroupName(Guid jobId) => GroupName(jobId.ToString());
    }
}