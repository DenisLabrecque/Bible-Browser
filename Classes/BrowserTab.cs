using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace BibleBrowser
{
   /// <summary>
   /// A tab that has a Bible and reference open.
   /// </summary>
   class BrowserTab : INotifyPropertyChanged
   {
      #region Property Changed Implementation

      public event PropertyChangedEventHandler PropertyChanged;

      // This method is called by the Set accessor of each property.  
      // The CallerMemberName attribute that is applied to the optional propertyName  
      // parameter causes the property name of the caller to be substituted as an argument.  
      private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      #endregion


      #region Constants and Static Elements

      private const string KEYHISTINDX = "tab_selected_index";
      private static ApplicationDataContainer m_localSettings = ApplicationData.Current.LocalSettings;

      /// <summary>
      /// A list of open tabs. TODO initialize at startup.
      /// </summary>
      public static TrulyObservableCollection<BrowserTab> Tabs = new TrulyObservableCollection<BrowserTab>();

      /// <summary>
      /// The currently selected browser tab.
      /// </summary>
      public static BrowserTab Selected {
         get {
            if (Tabs.Count == 0)
            {
               throw new Exception("No selected tab because there are no tabs");
            }
            else if (Tabs.Count - 1 < SelectedIndex)
            {
               SelectedIndex = Tabs.IndexOf(Tabs.LastOrDefault());
               return Tabs[SelectedIndex];
            }
            else
               return Tabs[SelectedIndex];
         }
      }

      /// <summary>
      /// The index of the currently open tab.
      /// Stored in application memory.
      /// </summary>
      public static int SelectedIndex {
         get {
            // First time the app is opened
            if (m_localSettings.Values[KEYHISTINDX] == null)
            {
               m_localSettings.Values[KEYHISTINDX] = Tabs.Count - 1;
               return Tabs.Count - 1;
            }
            else
               return (int)m_localSettings.Values[KEYHISTINDX];
         }
         set {
            if (value < 0)
            {
               throw new Exception("Cannot set the selected index to less than zero");
            }
            else if (value > Tabs.Count - 1)
               throw new Exception("Cannot set the selected index to more than the number of tabs");
            else
               m_localSettings.Values[KEYHISTINDX] = value;
         }
      }

      /// <summary>
      /// Return the next available reference, or null if there is none more.
      /// </summary>
      public BibleReference Next {
         get {
            if(m_HistoryIndex < History.Count - 1)
               return History[m_HistoryIndex + 1];
            else
               return null;
         }
      }

      /// <summary>
      /// Return the past visited reference, or null if there was none.
      /// </summary>
      public BibleReference Previous {
         get {
            if (m_HistoryIndex > 0)
               return History[m_HistoryIndex - 1];
            else
               return null;
         }
      }

      #endregion


      #region Member Variables

      private int m_HistoryIndex = 0;

      #endregion


      #region Properties

      /// <summary>
      /// A list of the current and previous references visited under this tab.
      /// The last item is most recently viewed.
      /// </summary>
      public List<BibleReference> History { get; private set; } = new List<BibleReference>();

      /// <summary>
      /// Get the currently active <c>BibleReference</c> from history.
      /// If there is none, return <c>null</c>.
      /// </summary>
      public BibleReference Reference { get {
            if (History.Count > 0)
               return History[m_HistoryIndex];
            else
               //throw new Exception("Reference is null");
               return null;
         }
         set {
            if (value == null)
               throw new Exception("Do not assign null to a Bible reference");
            else
            {
               History.Add(value);
               m_HistoryIndex = History.Count - 1;
               NotifyPropertyChanged();
            }
         }
      }

      /// <summary>
      /// Uniquely identify each tab.
      /// </summary>
      public Guid Guid { get; private set; }

      #endregion


      #region Constructor

      /// <summary>
      /// Static constructor.
      /// List browser tabs that were open in the past. /// TODO
      /// </summary>
      static BrowserTab()
      {
         Tabs.Add(
            new BrowserTab(
               new BibleReference(
                  BibleLoader.Version(BibleLoader.m_BibleFileNames[0]), BibleBook.Gn, 1, 1)));
         Tabs.Add(
            new BrowserTab(
               new BibleReference(
                  BibleLoader.Version(BibleLoader.m_BibleFileNames[0]), BibleBook.Rm, 1)));
         Tabs.Add(
            new BrowserTab(
               new BibleReference(
                  BibleLoader.Version(BibleLoader.m_BibleFileNames[0]), BibleBook.Lc, 1)));
         Tabs.Add(
            new BrowserTab(
               new BibleReference(
                  BibleLoader.Version(BibleLoader.m_BibleFileNames[0]), BibleBook.Rev, 22)));
      }

      /// <summary>
      /// Create a new browser tab with the default <c>BibleReference</c>.
      /// </summary>
      public BrowserTab(BibleReference reference)
      {
         if(reference == null)
            throw new ArgumentNullException("A new browser tab without a reference should be created with the overloaded constructor.");
         else
            History.Add(reference);
         Guid = Guid.NewGuid();
      }

      /// <summary>
      /// Create a new blank browser tab.
      /// </summary>
      public BrowserTab()
      {
         // Do not add anything to history yet.
         Guid = Guid.NewGuid();
      }

      #endregion


      #region Methods

      /// <summary>
      /// Overridden string method.
      /// </summary>
      /// <returns>A bible reference</returns>
      public override string ToString()
      {
         BibleReference reference = History.LastOrDefault(); // TODO
         if (reference == null)
            return Guid.ToString();
         else
            return "[" + reference.Version + "] " + reference.SimplifiedReference;
      }

      #endregion
   }
}
