using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;

namespace BibleBrowser
{
   /// <summary>
   /// A summary of metadata representing an XML Bible document.
   /// A list of these is created on load.
   /// </summary>
   class BibleVersion
   {
      #region Members

      string m_language;
      string m_filePath;
      string m_versionName;
      string m_versionAbbreviation;

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

      #endregion


      #region Constructor

      public BibleVersion(string fileName)
      {
         m_filePath = Path.Combine(Package.Current.InstalledLocation.Path, BiblesLoaded.BIBLE_PATH + "/" + fileName);

         // Create the XML document object
         XDocument loadedData = XDocument.Load(m_filePath);

         // Set information from XML
         m_language = loadedData.Root.Element(Zefania.NDE_INFO).Element(Zefania.NDE_LANG).Value;
         m_versionName = loadedData.Root.Attribute(Zefania.ATTR_BIBLENAME).Value;
         m_versionAbbreviation = loadedData.Root.Element(Zefania.NDE_INFO).Element(Zefania.VERSION_ABBR).Value;

         // Find the book names in XML
         foreach(XElement element in loadedData.Descendants(Zefania.NDE_BIBLEBOOK))
            m_bookNames.Add(element.Attribute(Zefania.ATTR_BOOKNAME).Value);

         // Find the book short names in XML
         foreach(XElement element in loadedData.Descendants(Zefania.NDE_BIBLEBOOK))
            m_bookAbbreviations.Add(element.Attribute(Zefania.ATTR_BOOKSHORTNAME).Value);

         // Find the book numbers in XML
         foreach(XElement element in loadedData.Descendants(Zefania.NDE_BIBLEBOOK))
            m_bookNumbers.Add(Int32.Parse(element.Attribute(Zefania.ATTR_BOOKNUM).Value));
      }

      #endregion


      #region Methods

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
