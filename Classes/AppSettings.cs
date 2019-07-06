using Windows.Storage;
using Windows.UI.Xaml;

namespace BibleBrowserUWP
{
   class AppSettings
   {
      const string KEY_THEME = "appColourMode";
      static ApplicationDataContainer LOCALSETTINGS = ApplicationData.Current.LocalSettings;

      /// <summary>
      /// Gets or sets the current app colour setting from memory (light or dark mode).
      /// </summary>
      public static ElementTheme Theme {
         get {
            // Never set: default to light mode
            if (LOCALSETTINGS.Values[KEY_THEME] == null)
            {
               LOCALSETTINGS.Values[KEY_THEME] = (int)ElementTheme.Light;
               return ElementTheme.Light;
            }
            // Previously set to light mode
            else if ((int)LOCALSETTINGS.Values[KEY_THEME] == (int)ElementTheme.Light)
            {
               return ElementTheme.Light;
            }
            // Previously set to dark mode
            else
            {
               return ElementTheme.Dark;
            }
         }
         set {
            // Error check
            if (value == ElementTheme.Default)
               throw new System.Exception("Only set the theme to light or dark mode!");
            // No change
            else if ((int)value == (int)LOCALSETTINGS.Values[KEY_THEME])
               return;
            // Change
            else
            {
               // Store the new setting
               LOCALSETTINGS.Values[KEY_THEME] = (int)value;
            }
         }
      }
   }
}
