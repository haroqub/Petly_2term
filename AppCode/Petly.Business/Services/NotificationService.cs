using Microsoft.EntityFrameworkCore;
using Petly.DataAccess.Data;
using Petly.Models;

namespace Petly.Business.Services;

public class NotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Notification>> GetUserNotifications(int userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task CreateAsync(int userId, string message, string type)
    {
        var notification = new Notification
        {
            UserId = userId,
            Message = message,
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task MarkAsRead(int id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        if (notification == null) return;

        notification.IsRead = true;
        await _context.SaveChangesAsync();
    }

    public async Task Delete(int id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        if (notification == null) return;

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();
    }

   
    public async Task MarkAllAsRead(int userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in notifications)
        {
            n.IsRead = true;
        }

        await _context.SaveChangesAsync();
    }


    public async Task<int> GetUnreadCount(int userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }
}