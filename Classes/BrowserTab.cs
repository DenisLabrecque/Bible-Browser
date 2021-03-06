﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using BibleBrowserUWP;

namespace BibleBrowserUWP
{
   /// <summary>
   /// A tab that has a Bible and reference open.
   /// </summary>
   class BrowserTab : INotifyPropertyChanged
   {
      public enum NavigationMode { Previous, Add, Next }

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
      private static StorageFolder m_localFolder = ApplicationData.Current.LocalFolder;

      /// <summary>
      /// A list of open tabs.
      /// </summary>
      public static TrulyObservableCollection<BrowserTab> Tabs = new TrulyObservableCollection<BrowserTab>();

      /// <summary>
      /// The currently selected browser tab. May return null.
      /// </summary>
      public static BrowserTab Selected {
         get {
            if (Tabs.Count == 0)
            {
               return null;
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
      /// Return the past visited reference, or null if there is none.
      /// </summary>
      public BibleReference Previous {
         get {
            if (m_HistoryIndex > 0)
               return History[m_HistoryIndex - 1];
            else
               return null;
         }
      }

      /// <summary>
      /// All available versions except the present one.
      /// </summary>
      public ObservableCollection<BibleVersion> OtherVersions {
         get {
            ObservableCollection<BibleVersion> versions = BibleLoader.Bibles;
            versions.Remove(Reference.Version);
            return versions;
         }
      }

      #endregion


      #region Member Variables

      private int m_HistoryIndex = 0;

      #endregion


      #region Properties

      public bool IsNewTab { get; private set; }

      /// <summary>
      /// A list of the current and previous references visited under this tab.
      /// The last item is most recently viewed.
      /// </summary>
      public List<BibleReference> History { get; private set; } = new List<BibleReference>();

      /// <summary>
      /// Get the currently active <c>BibleReference</c> from history.
      /// If there is none, return <c>null</c>.
      /// </summary>
      public BibleReference Reference {
         get {
            if (History.Count > 0)
               return History[m_HistoryIndex];
            else
               //throw new Exception("Reference is null");
               return null;
         }
      }

      /// <summary>
      /// The reference this tab is at, or a new tab appelation.
      /// </summary>
      public string TabName {
         get {
            if(IsNewTab)
               return "New tab";
            else
            {
               return Reference.BookName + " " + Reference.Chapter;
            }
         }
      }

      /// <summary>
      /// Get the language code of the current version, like "en" or "fr"
      /// </summary>
      public string LanguageCode {
         get {
            return Reference.Version.Language.ToLower();
         }
      }


      /// <summary>
      /// Add a reference to history as necessary.
      /// </summary>
      /// <param name="reference">The reference to visit; do not pass in <c>null</c>.
      /// This <c>ref</c> parameter returns the navigation result (so it can be navigated to).</param>
      /// <param name="mode">When the <c>NavigationMode</c> is <c>Previous</c> or <c>Next</c>, the reference is moved to but not added in history.</param>
      public void AddToHistory(ref BibleReference reference, NavigationMode mode = NavigationMode.Add)
      {
         if (reference == null)
            throw new Exception("Do not assign null to a Bible reference in history");
         else
         {
            switch(mode)
            {
               case NavigationMode.Add:
                  // Delete history that's past the current item
                  for (int i = m_HistoryIndex; i < History.Count; i++)
                  {
                     if (i > m_HistoryIndex)
                        History.RemoveAt(i);
                  }
                  History.Add(reference);
                  m_HistoryIndex = History.Count - 1;
                  NotifyPropertyChanged();
                  return;

               case NavigationMode.Previous:
                  m_HistoryIndex = Math.Clamp(m_HistoryIndex - 1, 0, History.Count - 1);
                  reference = History[m_HistoryIndex];
                  NotifyPropertyChanged();
                  return;

               case NavigationMode.Next:
                  m_HistoryIndex = Math.Clamp(m_HistoryIndex + 1, 0, History.Count - 1);
                  reference = History[m_HistoryIndex];
                  NotifyPropertyChanged();
                  return;
            }

            foreach(var item in History)
            {
               Debug.WriteLine("History item: " + item.FullReference + " " + item.IsSearch);
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
      /// Create a custom browser tab.
      /// </summary>
      public BrowserTab(BibleReference reference, bool isNewTab)
      {
         Worker(reference, IsNewTab);
      }

      /// <summary>
      /// Create a pre-existing browser tab.
      /// </summary>
      public BrowserTab(BibleReference reference)
      {
         Worker(reference, false);
      }

      /// <summary>
      /// Create a new tab with the default reference.
      /// </summary>
      public BrowserTab()
      {
         Worker(BibleReference.Default, true);
      }

      private void Worker(BibleReference reference, bool isNewTab)
      {
         if (reference == null)
            throw new ArgumentNullException("A new browser tab without a reference should be created with the overloaded constructor.");
         else
            History.Add(reference);
         Guid = Guid.NewGuid();
         IsNewTab = IsNewTab;
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

      /// <summary>
      /// Save the presently open tabs to an XML document.
      /// </summary>
      public static async Task SaveOpenTabs()
      {
         // Create a new XML document root node
         XDocument document = new XDocument(new XElement("SavedTabs"));

         // Save each open tab to the XML document
         foreach (BrowserTab tab in Tabs)
         {
            if (tab.Reference != null)
            {
               // Check, otherwise throwing in null will fail the constructor
               string comparisonVersion;
               if(tab.Reference.ComparisonVersion == null)
               {
                  comparisonVersion = "Null";
               }
               else
               {
                  comparisonVersion = tab.Reference.ComparisonVersion.FileName;
               }

               XElement element = new XElement("Reference",
                  new XElement("FileName", tab.Reference.Version.FileName),
                  new XElement("ComparisonFileName", comparisonVersion),
                  new XElement("BookName", tab.Reference.BookName),
                  new XElement("Chapter", tab.Reference.Chapter),
                  new XElement("Verse", tab.Reference.Verse)
               );
               document.Root.Add(element);
            }
         }

         // Debug the file contents
         Debug.WriteLine("Tabs saved as :");
         Debug.WriteLine(document);

         // Save the resulting XML document
         StorageFile writeFile = await m_localFolder.CreateFileAsync("SavedTabs.xml", CreationCollisionOption.ReplaceExisting);
         await FileIO.WriteTextAsync(writeFile, document.ToString());
      }


      /// <summary>
      /// Initialize the tabs to when the browser was previously open.
      /// Assign the tabs to the loaded data.
      /// </summary>
      public static async Task LoadSavedTabs()
      {
         // There may not be a saved document
         try
         {
            // Read the saved XML tabs
            StorageFile readFile = await m_localFolder.GetFileAsync("SavedTabs.xml");
            string text = await FileIO.ReadTextAsync(readFile);
            // Debug file contents
            Debug.WriteLine("The saved tabs xml file contents :");
            Debug.WriteLine(text);
            XDocument XMLTabs = XDocument.Parse(text);

            // Debug the file contents
            Debug.WriteLine("Tabs loaded :");
            Debug.WriteLine(XMLTabs);

            // Create the tab list from XML
            IEnumerable<XElement> tabs = XMLTabs.Descendants("Reference");
            TrulyObservableCollection<BrowserTab> savedTabs = new TrulyObservableCollection<BrowserTab>();
            foreach (XElement node in tabs)
            {
               // Get the information from XML
               BibleVersion bibleVersion = new BibleVersion(node.Element("FileName").Value);
               BibleVersion comparisonVersion;
               if(node.Element("ComparisonFileName").Value == "Null")
               {
                  comparisonVersion = null;
               }
               else
               {
                  comparisonVersion = new BibleVersion(node.Element("ComparisonFileName").Value);
               }
               string bookName = node.Element("BookName").Value;
               BibleBook book = BibleReference.StringToBook(bookName, bibleVersion);
               int chapter = int.Parse(node.Element("Chapter").Value);
               int verse = int.Parse(node.Element("Verse").Value);

               // Create the reference that goes in the tab
               BibleReference reference = new BibleReference(bibleVersion, comparisonVersion, book, chapter, verse);
               savedTabs.Add(new BrowserTab(reference));
            }

            // Add the tabs to the browser
            foreach (BrowserTab tab in savedTabs)
            {
               Tabs.Add(tab);
            }

            RequireNewTab();
         }
         catch(System.IO.FileNotFoundException fileNotFoundE)
         {
            Debug.WriteLine("A resource was not loaded correctly; this may be a missing bible version :");
            Debug.WriteLine(fileNotFoundE.Message);

            RequireNewTab();
         }
         catch (System.Xml.XmlException xmlE) // Parse error
         {
            Debug.WriteLine("Reading the saved tabs xml file choked :");
            Debug.WriteLine(xmlE.Message);

            RequireNewTab();
         }
         catch (Exception e)
         {
            Debug.WriteLine("Loading saved tabs was interrupted :");
            Debug.WriteLine(e.Message);

            RequireNewTab();
         }
      }

      /// <summary>
      /// Ensure that there is at least one tab open, even when none existed in app memory.
      /// </summary>
      private static void RequireNewTab()
      {
         // Avoid loading no tabs at first startup
         if (Tabs.Count == 0)
         {
            Tabs.Add(new BrowserTab());
         }
      }

      #endregion
   }
}
