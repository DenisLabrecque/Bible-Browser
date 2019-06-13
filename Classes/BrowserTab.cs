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
      public event PropertyChangedEventHandler PropertyChanged;

      // This method is called by the Set accessor of each property.  
      // The CallerMemberName attribute that is applied to the optional propertyName  
      // parameter causes the property name of the caller to be substituted as an argument.  
      private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      #region Constants and Static Elements

      private const string KEYHISTINDX = "current_history_index";
      private const string KEYHISTGUID = "current_tab_guid";
      private static ApplicationDataContainer m_localSettings = ApplicationData.Current.LocalSettings;

      /// <summary>
      /// A list of open tabs. TODO initialize at startup.
      /// </summary>
      public static BindingList<BrowserTab> Tabs = new BindingList<BrowserTab>();

      /// <summary>
      /// The currently selected browser tab.
      /// </summary>
      //public static BrowserTab Selected {
      //   get {
      //      if (Tabs.Count > 0)
      //         return Tabs[TabIndex];
      //      else
      //         throw new IndexOutOfRangeException("There is no tab");
      //   }
      //   set {
      //      if (Tabs.Count > 0)
      //      {
      //         if (value == null)
      //            throw new ArgumentNullException("A new tab should not be null");
      //         else
      //            Tabs[TabIndex] = value;
      //      }
      //      else
      //         throw new IndexOutOfRangeException("There is no tab");
      //   }
      //}

      public static BrowserTab Selected {
         get {
            if (Tabs != null && Tabs.Count > 0)
            {
               var result = Tabs.Where(p => p.Guid == CurrentTabGuid);
               if (result != null)
                  return result.ElementAtOrDefault(0); // There is only one element because a Guid is unique
               else
                  throw new Exception("Returned no result");
            }
            else
               throw new Exception("There are no tabs");
         }
         set {
            if (value != null)
            {
               CurrentTabGuid = value.Guid;
            }
            else
               throw new ArgumentNullException("Cannot assign a null browser tab as the current tab");
         }
      }

      /// <summary>
      /// The index of the currently open tab.
      /// Stored in application memory.
      /// </summary>
      //public static int TabIndex {
      //   get {
      //      // The newly opened tab has no reference
      //      if (m_localSettings.Values[KEYHISTINDX] == null)
      //         return 0;
      //      // There is no selection, or the last selection was a now closed tab
      //      else
      //         return Math.Clamp((int)m_localSettings.Values[KEYHISTINDX], 0, Tabs.Count - 1);
      //   }
      //   set {
      //      m_localSettings.Values[KEYHISTINDX] = Math.Clamp(value, 0, Tabs.Count - 1);
      //   }
      //}


      public static Guid CurrentTabGuid {
         get {
            // No tab was yet clicked
            if (m_localSettings.Values[KEYHISTGUID] == null)
            {
               Guid guid = Tabs.LastOrDefault().Guid;
               m_localSettings.Values[KEYHISTGUID] = guid;
               return guid;
            }
            // The current tab
            else
               return CurrentTabGuid;
         }
         set {
            if (value != null)
            {
               CurrentTabGuid = value;
            }
            else
               throw new ArgumentNullException("Attempted to assign a null Guid to the current tab Guid");
         }
      }

      #endregion


      #region Member Variables

      private int m_CurrentReferenceIndex = 0;

      #endregion


      #region Properties

      /// <summary>
      /// A list of the current and previous references visited under this tab.
      /// The last item is most recently viewed.
      /// </summary>
      public List<BibleReference> History { get; private set; } = new List<BibleReference>();

      /// <summary>
      /// Get the currently active <c>BibleReference</c> from history.
      /// </summary>
      public BibleReference Reference { get {
            if (History.Count > 0 && m_CurrentReferenceIndex < History.Count)
               return History[m_CurrentReferenceIndex];
            else
               throw new IndexOutOfRangeException("No tab history to access at index " + m_CurrentReferenceIndex);
         }
         set {
            if (value == null)
               throw new Exception("Reference attempted to set to null");
            else
            {
               NotifyPropertyChanged(Reference);
               History.Add(value);
               Reference = value;
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
            throw new ArgumentNullException("A new browser tab cannot be created with a null reference");
         else
            History.Add(reference);
         Guid = Guid.NewGuid();
      }

      /// <summary>
      /// Create a new blank browser tab.
      /// </summary>
      public BrowserTab()
      {
         History.Add(new BibleReference(null));
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
         if (History.Count > 0)
         {
            BibleReference reference = History.Last();
            return "[" + reference.Version + "] " + reference.SimplifiedReference;
         }
         else
            return "[NULL] null";
      }

      #endregion
   }
}
