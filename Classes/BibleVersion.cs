using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.ApplicationModel;

namespace BibleBrowser
{
   /// <summary>
   /// A summary of metadata and the document representing an XML Bible document.
   /// A list of these is created on load.
   /// </summary>
   class BibleVersion
   {
      #region Members

      string m_filePath;
      XDocument m_xDocument = null;
      static Windows.Storage.ApplicationDataContainer m_localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

      const string INDX_DEFAULTVERSION = "default_version";

      #endregion


      #region Properties

      public string Language { get; }
      public string Title { get; }
      public string Abbreviation { get; }
      public List<string> BookNames { get; } = new List<string>();
      public List<string> BookAbbreviations { get; } = new List<string>();
      public List<int> BookNumbers { get; } = new List<int>();

      /// <summary>
      /// The xml document file name + extension (excluding the path).
      /// </summary>
      public string FileName { get; private set; }

      /// <summary>
      /// Automatically loads a Bible's XDocument when needed.
      /// </summary>
      public XDocument XDocument {
         get {
            if (m_xDocument == null)
               m_xDocument = XDocument.Load(m_filePath);
            return m_xDocument;
         }
      }

      /// <summary>
      /// Return the default Bible version.
      /// </summary>
      public static BibleVersion DefaultVersion {
         get {
            if(m_localSettings.Values[INDX_DEFAULTVERSION] == null)
            {
               BibleVersion version = BibleLoader.LoadedBibles.FirstOrDefault().Value;
               SetDefaultVersion(version.FileName);
               return BibleLoader.LoadedBibles.FirstOrDefault().Value;
            }
            else
            {
               string filename = (string)m_localSettings.Values[INDX_DEFAULTVERSION];
               return BibleLoader.LoadedBibles[filename];
            }
         }
      }

      #endregion


      #region Constructor

      /// <summary>
      /// Constructor which loads a known Bible version <c>XDocument</c> and sets metadata about it.
      /// </summary>
      /// <param name="fileName">The Bible version file name wanted to load.</param>
      public BibleVersion(string fileName)
      {
         FileName = fileName;
         m_filePath = Path.Combine(Package.Current.InstalledLocation.Path, BibleLoader.BIBLE_PATH + "/" + fileName);

         // Set information from XML
         Language = XDocument.Root.Element(Zefania.NDE_INFO).Element(Zefania.NDE_LANG).Value;
         Title = XDocument.Root.Element(Zefania.NDE_INFO).Element(Zefania.NDE_TITLE).Value;
         Abbreviation = XDocument.Root.Element(Zefania.NDE_INFO).Element(Zefania.VERSION_ABBR).Value;

         // Find the book names in XML
         foreach(XElement element in XDocument.Descendants(Zefania.NDE_BIBLEBOOK))
            BookNames.Add(element.Attribute(Zefania.ATTR_BOOKNAME).Value);

         // Find the book short names in XML
         foreach(XElement element in XDocument.Descendants(Zefania.NDE_BIBLEBOOK))
            BookAbbreviations.Add(element.Attribute(Zefania.ATTR_BOOKSHORTNAME).Value);

         // Find the book numbers in XML
         foreach(XElement element in XDocument.Descendants(Zefania.NDE_BIBLEBOOK))
            BookNumbers.Add(Int32.Parse(element.Attribute(Zefania.ATTR_BOOKNUM).Value));
      }

      #endregion


      #region Methods

      /// <summary>
      /// Set the default version in app setting memory.
      /// </summary>
      /// <param name="fileName">The file name + file extension of an xml Bible</param>
      public static void SetDefaultVersion(string fileName)
      {
         if (fileName == null)
         {
            throw new ArgumentNullException("Cannot set the default Bible version to a null value.");
         }
         else
         {
            if (BibleLoader.LoadedBibles.ContainsKey(fileName))
            {
               m_localSettings.Values[INDX_DEFAULTVERSION] = fileName;
            }
            else
            {
               throw new ArgumentException("The file name " + fileName + " is not a loaded Bible version, so it cannot be set as a default.");
            }
         }
      }

      /// <summary>
      /// Get every verse in a chapter by <code>BibleReference</code>.
      /// </summary>
      /// <param name="reference">The chapter to look up</param>
      /// <returns>A list of strings that are the contents of each verse in the Bible's chapter.</returns>
      public List<string> GetChapterVerses(BibleReference reference)
      {
         List<string> verseContents = new List<string>();
         if (reference == null)
            return verseContents; // Blank page
         
         XElement book = XDocument.Descendants("BIBLEBOOK").ElementAt((int)reference.Book);
         XElement chapter = book.Descendants("CHAPTER").ElementAt(reference.Chapter - 1);
         
         foreach(XElement element in chapter.Elements())
         {
            verseContents.Add(element.Value.FormatTypographically());
         }

         return verseContents;
      }


      /// <summary>
      /// Find how many chapters are in a certain book of the Bible.
      /// </summary>
      /// <param name="reference">The book to look up (chapter number ignored).</param>
      /// <returns>The number of chapters in the book.</returns>
      public int GetChapterCount(BibleReference reference)
      {
         XElement book = XDocument.Descendants("BIBLEBOOK").ElementAt((int)reference.Book);
         return book.Descendants("CHAPTER").Count();
      }


      /// <summary>
      /// Find how many verses are in a certain book chapter of the Bible.
      /// </summary>
      /// <param name="reference">The book and chapter to look up (verse number ignored).</param>
      /// <returns>The number of verses in the chapter.</returns>
      public int GetVerseCount(BibleReference reference)
      {
         XElement book = XDocument.Descendants("BIBLEBOOK").ElementAt((int)reference.Book);
         XElement chapter = book.Descendants("CHAPTER").ElementAt(reference.Chapter - 1);
         return chapter.Descendants("VERS").Count();
      }


      /// <summary>
      /// Overridden string method.
      /// </summary>
      /// <returns>The version's abbreviation. For the full version name, use the property.</returns>
      public override string ToString()
      {
         return Abbreviation;
      }

      #endregion
   }
}
