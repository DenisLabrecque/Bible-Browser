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
      /// The index of the currently open tab.
      /// Stored in application memory.
      /// </summary>
      public static int TabIndex {
         get {
            if (m_localSettings.Values[KEYHISTINDX] == null)
               return 0;
            else
               return (int)m_localSettings.Values[KEYHISTINDX];
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

      public BibleReference Reference { get => History[m_CurrentReferenceIndex]; }

      #endregion


        #region Initialize

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

        #endregion


      #region Constructor

      /// <summary>
      /// Create a new browser tab with the default BibleReference.
      /// </summary>
      public BrowserTab(BibleReference reference)
      {
         if(reference == null)
            throw new ArgumentNullException("A new browser tab cannot be created using a null BibleReference");
         else
            History.Add(reference);
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
