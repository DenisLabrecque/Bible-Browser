using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace BibleBrowser
{
   /// <summary>
   /// Represents XML Bible with all its metadata, and exposes the capacity to retrieve both meta information
   /// and content from those Bibles.
   /// </summary>
   static class BibleLoader
   {
      #region Properties

      /// <summary>
      /// Maintains a list of the XML Bible names and book names in the versions' languages.
      /// </summary>
      public static Dictionary<string, BibleVersion> LoadedBibles { get; private set; } = new Dictionary<string, BibleVersion>();

      /// <summary>
      /// The <c>BibleVersion</c>s available to read from.
      /// </summary>
      public static List<BibleVersion> Bibles {
         get {
            List<BibleVersion> bibles = new List<BibleVersion>();
            foreach (var bible in LoadedBibles)
            {
               bibles.Add(bible.Value);
            }
            return bibles;
         }
      }

      /// <summary>
      /// A list of all the local files that contain Bibles we want to read from.
      /// </summary>
      public static readonly List<string> m_BibleFileNames = new List<string>() {
         "ylt.xml",
         "lsg.xml"
      };

      /// <summary>
      /// The app folder in which Bible versions are stored.
      /// </summary>
      public const string BIBLE_PATH = "Bibles";

      #endregion


      #region Methods

      /// <summary>
      /// Static constructor.
      /// Add each Bible in the app directory to the list of Bibles.
      /// </summary>
      static BibleLoader()
      {
         // Create a new meta object for each Bible for fast retrieval.
         foreach (string fileName in m_BibleFileNames)
         {
            LoadedBibles.Add(fileName, new BibleVersion(fileName));
         }
      }


      /// <summary>
      /// Get the <c>BibleVersion</c> loaded into memory that matches a file name.
      /// </summary>
      /// <param name="fileName">The file name of a version</param>
      /// <returns>A <c>BibleVersion</c></returns>
      public static BibleVersion Version(string fileName)
      {
         return LoadedBibles[fileName];
      }


      /// <summary>
      /// Get the tag name of XML
      /// May return null...
      /// Ajithsz, How to get Tag name of Xml by using XDocument
      /// </summary>
      /// <param name="doc">The XML document</param>
      /// <param name="elementName">The tag name</param>
      /// <returns></returns>
      private static XElement GetElement(XDocument doc, string elementName)
      {
         foreach(XNode node in doc.DescendantNodes())
         {
            if(node is XElement)
            {
               XElement element = (XElement)node;
               if(element.Name.LocalName.Equals(elementName))
                  return element;
            }
         }
         return null;
      }

      #endregion


      #region Tests

      public static String TestInformation()
      {
         string info = null;

         foreach(KeyValuePair<string, BibleVersion> bible in LoadedBibles)
         {
            info = bible.Value.Language + ", " + bible.Value.Abbreviation + ", " + bible.Value.Title;
         }

         return info;
      }

      /// <summary>
      /// Show all the loaded book names from every available Bibles concatenated together.
      /// </summary>
      /// <returns></returns>
      public static String TestBookNames()
      {
         string bookNames = string.Empty;

         foreach(KeyValuePair<string, BibleVersion> bible in LoadedBibles)
         {
            List<String> bknames = bible.Value.BookNames;

            foreach(String name in bknames)
            {
                  bookNames += name + "\n";
            }
         }

         return bookNames;
      }
      #endregion
   }
}