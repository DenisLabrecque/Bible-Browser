using System;
using Windows.Storage;
using Windows.UI.Xaml;

namespace BibleBrowserUWP
{
   class AppSettings
   {
      public const ElementTheme DEFAULTTHEME = ElementTheme.Light;
      public const ElementTheme NONDEFLTHEME = ElementTheme.Dark;
      public const bool NONOTIFICATIONS = false;
      public static readonly TimeSpan NOTIFICATIONTIME = new TimeSpan(8, 0, 0);

      const string KEY_THEME = "appColourMode";
      const string KEY_NOTIFICATIONS = "readingNotifications";
      const string KEY_NOTIFYTIME = "toastNotificationTime";
      static ApplicationDataContainer LOCALSETTINGS = ApplicationData.Current.LocalSettings;

      /// <summary>
      /// Gets or sets the current app colour setting from memory (light or dark mode).
      /// </summary>
      public static ElementTheme Theme {
         get {
            // Never set: default theme
            if (LOCALSETTINGS.Values[KEY_THEME] == null)
            {
               LOCALSETTINGS.Values[KEY_THEME] = (int)DEFAULTTHEME;
               return DEFAULTTHEME;
            }
            // Previously set to default theme
            else if ((int)LOCALSETTINGS.Values[KEY_THEME] == (int)DEFAULTTHEME)
               return DEFAULTTHEME;
            // Previously set to non-default theme
            else
               return NONDEFLTHEME;
         }
         set {
            // Error check
            if (value == ElementTheme.Default)
               throw new Exception("Only set the theme to light or dark mode!");
            // Never set
            else if (LOCALSETTINGS.Values[KEY_THEME] == null)
               LOCALSETTINGS.Values[KEY_THEME] = (int)value;
            // No change
            else if ((int)value == (int)LOCALSETTINGS.Values[KEY_THEME])
               return;
            // Change
            else
               LOCALSETTINGS.Values[KEY_THEME] = (int)value;
         }
      }

      /// <summary>
      /// Whether toast notifications should remind the user to read his Bible.
      /// </summary>
      public static bool ReadingNotifications {
         get {
            // Never set: default to no notifications
            if (LOCALSETTINGS.Values[KEY_NOTIFICATIONS] == null)
            {
               LOCALSETTINGS.Values[KEY_NOTIFICATIONS] = NONOTIFICATIONS;
               return NONOTIFICATIONS;
            }
            // Previously set to default to no notifications
            else if ((bool)LOCALSETTINGS.Values[KEY_NOTIFICATIONS] == NONOTIFICATIONS)
               return NONOTIFICATIONS;
            // Previously set to notify
            else
               return !NONOTIFICATIONS;
         }
         set {
            // Never set
            if (LOCALSETTINGS.Values[KEY_NOTIFICATIONS] == null)
               LOCALSETTINGS.Values[KEY_NOTIFICATIONS] = value;
            // No change
            else if (value == (bool)LOCALSETTINGS.Values[KEY_NOTIFICATIONS])
               return;
            // Change
            else
               LOCALSETTINGS.Values[KEY_NOTIFICATIONS] = value;
         }
      }

      /// <summary>
      /// What time the user wants to be notified to read his Bible.
      /// </summary>
      public static TimeSpan NotifyTime {
         get {
            // Never set: set to default time
            if (LOCALSETTINGS.Values[KEY_NOTIFYTIME] == null)
            {
               LOCALSETTINGS.Values[KEY_NOTIFYTIME] = NOTIFICATIONTIME;
               return NOTIFICATIONTIME;
            }
            // Previously set
            else
               return (TimeSpan)LOCALSETTINGS.Values[KEY_NOTIFYTIME];
         }
         set {
            // Error check
            if (value == null)
               throw new ArgumentNullException("The date passed as a notification time cannot be null");
            else
               LOCALSETTINGS.Values[KEY_NOTIFYTIME] = value;
         }
      }
   }
}
