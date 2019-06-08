using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BibleBrowser
{
   /// <summary>
   /// A tab that has a Bible and reference open.
   /// </summary>
   class BrowserTab
   {
      #region Static Members

      /// <summary>
      /// A list of open tabs.
      /// Initialized at startup.
      /// </summary>
      public static ObservableCollection<BrowserTab> Tabs = new ObservableCollection<BrowserTab>();

      #endregion


      #region Properties

      /// <summary>
      /// The index of the current reference.
      /// </summary>
      public int CurrentHistoryIndex { get; private set; } = 0;

      /// <summary>
      /// A list of the current and previous references visited under this tab.
      /// The last item is most recently viewed.
      /// </summary>
      public List<BibleReference> History { get; private set; } = new List<BibleReference>();

      public BibleReference Reference { get => History[CurrentHistoryIndex]; }

      #endregion


        #region Initialize

        /// <summary>
        /// Static constructor.
        /// List browser tabs that were open in the past.
        /// TODO
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
                     BibleLoader.Version(BibleLoader.m_BibleFileNames[0]), BibleBook.Rm, 2)));
            Tabs.Add(
               new BrowserTab(
                  new BibleReference(
                     BibleLoader.Version(BibleLoader.m_BibleFileNames[0]), BibleBook.Lc)));
            Tabs.Add(
               new BrowserTab(
                  new BibleReference(
                     BibleLoader.Version(BibleLoader.m_BibleFileNames[0]), BibleBook.Sng)));
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
      /// Overridden string method.
      /// </summary>
      /// <returns>A bible reference</returns>
      public override string ToString()
      {
         BibleReference reference = History[CurrentHistoryIndex];
         return "[" + reference.Version + "] " + reference.SimplifiedReference;
      }

      #endregion
   }
}
