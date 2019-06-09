using System;
using System.Collections.Generic;
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

      string m_language;
      string m_filePath;
      string m_versionName;
      string m_versionAbbreviation;

      XDocument m_xDocument = null;

      List<string> m_bookNames = new List<string>();
      List<string> m_bookAbbreviations = new List<string>();
      List<int> m_bookNumbers = new List<int>();

      #endregion


      #region Properties

      public string Language { get => m_language; }
      public string VersionName { get => m_versionName; }
      public string VersionAbbreviation { get => m_versionAbbreviation; }
      public List<string> BookNames { get => m_bookNames; }
      public List<string> BookAbbreviations { get => m_bookAbbreviations; }
      public List<int> BookNumbers { get => m_bookNumbers; }

      public XDocument XDocument {
         get {
            if(m_xDocument == null)
               m_xDocument = XDocument.Load(m_filePath);
            return m_xDocument;
         }
      }

      #endregion


      #region Constructor

      public BibleVersion(string fileName)
      {
         m_filePath = Path.Combine(Package.Current.InstalledLocation.Path, BibleLoader.BIBLE_PATH + "/" + fileName);

         // Set information from XML
         m_language = XDocument.Root.Element(Zefania.NDE_INFO).Element(Zefania.NDE_LANG).Value;
         m_versionName = XDocument.Root.Attribute(Zefania.ATTR_BIBLENAME).Value;
         m_versionAbbreviation = XDocument.Root.Element(Zefania.NDE_INFO).Element(Zefania.VERSION_ABBR).Value;

         // Find the book names in XML
         foreach(XElement element in XDocument.Descendants(Zefania.NDE_BIBLEBOOK))
            m_bookNames.Add(element.Attribute(Zefania.ATTR_BOOKNAME).Value);

         // Find the book short names in XML
         foreach(XElement element in XDocument.Descendants(Zefania.NDE_BIBLEBOOK))
            m_bookAbbreviations.Add(element.Attribute(Zefania.ATTR_BOOKSHORTNAME).Value);

         // Find the book numbers in XML
         foreach(XElement element in XDocument.Descendants(Zefania.NDE_BIBLEBOOK))
            m_bookNumbers.Add(Int32.Parse(element.Attribute(Zefania.ATTR_BOOKNUM).Value));
      }

      #endregion


      #region Methods

      /// <summary>
      /// Get every verse in a chapter by <c>BibleReference</c>.
      /// </summary>
      /// <param name="reference">The chapter to look up</param>
      /// <returns>A list of strings that are the contents of each verse in the Bible's chapter.</returns>
      public List<string> GetChapterVerses(BibleReference reference)
      {
         List<string> verseContents = new List<string>();
         XElement book = XDocument.Descendants("BIBLEBOOK").ElementAt((int)reference.Book);
         XElement chapter = book.Descendants("CHAPTER").ElementAt(reference.Chapter - 1);
         
         foreach(XElement element in chapter.Elements())
         {
            verseContents.Add(element.Value);
         }

         return verseContents;
      }


      /// <summary>
      /// Overridden string method.
      /// </summary>
      /// <returns>The version's abbreviation. For the full version name, use the property.</returns>
      public override string ToString()
      {
         return VersionAbbreviation;
      }

      #endregion
   }
}
