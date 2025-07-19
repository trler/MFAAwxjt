using Avalonia.Controls.Notifications;
using DesktopNotifications.Apple;
using DesktopNotifications.FreeDesktop;
using DesktopNotifications.Windows;
using MFAAvalonia.Helper;
using System;
using System.Collections.Generic;
using INotificationManager = DesktopNotifications.INotificationManager;
using Notification = DesktopNotifications.Notification;

namespace MFAAvalonia.Helper;

public static class ToastNotification
{
    private static INotificationManager GetNotificationManager()
    {

        if (OperatingSystem.IsWindows())
        {
            return new WindowsNotificationManager();
        }
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
        {
            return new AppleNotificationManager();
        }
        if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
        {
            return new FreeDesktopNotificationManager();
        }

        throw new PlatformNotSupportedException("Current system does not support any available notification manager");
    }

    public static void Show(string title = "", string message = "")
    {
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
        {
            return;
        }
        try
        {
            var notificationManager = GetNotificationManager();

            var notification = new Notification
            {
                Title = title,
                Body = message
            };

            notificationManager.ShowNotification(notification);
        }
        catch (Exception e)
        {
            LoggerHelper.Error(e);
        }
    }
}
