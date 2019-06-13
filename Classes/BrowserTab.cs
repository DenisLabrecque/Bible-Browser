using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace BibleBrowser
{
   /// <summary>
   /// A tab that has a Bible and reference open.
   /// </summary>
   class BrowserTab
   {
      #region Constants and Static Elements

      private const string KEYHISTINDX = "current_history_index";
      private static ApplicationDataContainer m_localSettings = ApplicationData.Current.LocalSettings;

      /// <summary>
      /// A list of open tabs. TODO initialize at startup.
      /// </summary>
      public static ObservableCollection<BrowserTab> Tabs = new ObservableCollection<BrowserTab>();

      /// <summary>
      /// The currently selected browser tab.
      /// </summary>
      public static BrowserTab Selected { get => Tabs[TabIndex]; }

      /// <summary>
      /// The index of the currently open tab.
      /// Stored in application memory.
      /// </summary>
      public static int TabIndex {
         get {
            int? index = (int)m_localSettings.Values[KEYHISTINDX];
            if (index == null)
               return 0;
            else
            {
               if (index > Tabs.Count - 1)
                  return Tabs.Count - 1;
               else
                  return (int)index;
            }
         }
         private set {
            m_localSettings.Values[KEYHISTINDX] = value;
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
      /// If there is none, return <c>null</c>.
      /// </summary>
      public BibleReference Reference { get {
            if (History.Count > 0)
               return History[m_CurrentReferenceIndex];
            else
               return null;
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
      /// TODO if this tab had history in the past, re-import the history from app memory
      /// </summary>
      public BrowserTab()
      {
         // Do not add anything to history yet.
         Guid = Guid.NewGuid();
      }

      #endregion


      #region Methods

      /// <summary>
      /// Set the index of the currently open tab property.
      /// This index is stored in app memory.
      /// </summary>
      /// <param name="index"></param>
      public static void SetTabIndex(int index)
      {
         TabIndex = index;
      }

      /// <summary>
      /// Overridden string method.
      /// </summary>
      /// <returns>A bible reference</returns>
      public override string ToString()
      {
         BibleReference reference = History[TabIndex];
         return "[" + reference.Version + "] " + reference.SimplifiedReference;
      }

      #endregion
   }
}
