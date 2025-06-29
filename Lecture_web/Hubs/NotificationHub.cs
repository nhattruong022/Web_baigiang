using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Lecture_web.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public async Task JoinClass(string classId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Class_{classId}");
        }

        public async Task LeaveClass(string classId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Class_{classId}");
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            }
            await base.OnDisconnectedAsync(exception);
        }

        // --- COMMENT REALTIME ---
        public async Task SendComment(string classId, object comment)
        {
            await Clients.Group($"Class_{classId}").SendAsync("ReceiveComment", comment);
        }

        public async Task UpdateComment(string classId, object comment)
        {
            await Clients.Group($"Class_{classId}").SendAsync("UpdateComment", comment);
        }

        public async Task DeleteComment(string classId, int commentId)
        {
            await Clients.Group($"Class_{classId}").SendAsync("DeleteComment", commentId);
        }
    }
} 