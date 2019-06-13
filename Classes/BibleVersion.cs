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

      string m_filePath;
      XDocument m_xDocument = null;

      #endregion


      #region Properties

      public string Language { get; }
      public string VersionName { get; }
      public string VersionAbbreviation { get; }
      public List<string> BookNames { get; } = new List<string>();
      public List<string> BookAbbreviations { get; } = new List<string>();
      public List<int> BookNumbers { get; } = new List<int>();

      public XDocument XDocument {
         get {
            if (m_xDocument == null)
               m_xDocument = XDocument.Load(m_filePath);
            return m_xDocument;
         }
      }

      #endregion


      #region Constructor

      /// <summary>
      /// Constructor with a known Bible version.
      /// </summary>
      /// <param name="fileName">The Bible version file name wanted to use.</param>
      public BibleVersion(string fileName)
      {
         m_filePath = Path.Combine(Package.Current.InstalledLocation.Path, BibleLoader.BIBLE_PATH + "/" + fileName);

         // Set information from XML
         Language = XDocument.Root.Element(Zefania.NDE_INFO).Element(Zefania.NDE_LANG).Value;
         VersionName = XDocument.Root.Attribute(Zefania.ATTR_BIBLENAME).Value;
         VersionAbbreviation = XDocument.Root.Element(Zefania.NDE_INFO).Element(Zefania.VERSION_ABBR).Value;

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
      /// Get every verse in a chapter by <c>BibleReference</c>.
      /// </summary>
      /// <param name="reference">The chapter to look up</param>
      /// <returns>A list of strings that are the contents of each verse in the Bible's chapter.</returns>
      public List<string> GetChapterVerses(BibleReference reference)
      {
         List<string> verseContents = new List<string>();
         XElement book = XDocument.Descendants("BIBLEBOOK").ElementAt(Math.Clamp((int)reference.Book, 0, (int)BibleBook.Rev));
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
