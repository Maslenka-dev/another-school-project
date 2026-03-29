using System;
using System.Drawing;
using Forms = System.Windows.Forms;

namespace ProductivityTimer.Services;

public sealed class NotificationService : INotificationService, IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private bool _disposed;

    public NotificationService()
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Information,
            Visible = true,
            Text = "Productivity Timer"
        };
    }

    public void Show(string title, string message)
    {
        if (_disposed)
        {
            return;
        }

        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(3000);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}


