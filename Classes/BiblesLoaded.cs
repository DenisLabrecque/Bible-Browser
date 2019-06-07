﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Xml.Linq;
using Windows.ApplicationModel;

namespace BibleBrowser
{
   /// <summary>
   /// Represents XML Bible with all its metadata, and exposes the capacity to retrieve both meta information
   /// and content from those Bibles.
   /// </summary>
   static class BiblesLoaded
   {
      #region Members

      /// <summary>
      /// Maintains a list of the XML Bible names and book names in the versions' languages.
      /// </summary>
      static Dictionary<string, BibleVersion> m_loadedBibles = new Dictionary<string, BibleVersion>();

      /// <summary>
      /// A list of all the local files that contain Bibles we want to read from.
      /// </summary>
      public static readonly List<string> m_BibleFileNames = new List<string>() {
         "ylt.xml"
      };

      /// <summary>
      /// The app folder in which Bible versions are stored.
      /// </summary>
      public const string BIBLE_PATH = "Bibles";

      #endregion


      #region Methods

      /// <summary>
      /// Add each Bible in the app directory to the list of Bibles.
      /// </summary>
      public static void Initialize()
      {
         foreach(string fileName in m_BibleFileNames)
            m_loadedBibles.Add(fileName, new BibleVersion(fileName)); // Create a new meta object for each Bible for fast retrieval.
      }


      /// <summary>
      /// Get the <c>BibleVersion</c> loaded into memory that matches a file name.
      /// </summary>
      /// <param name="fileName">The file name of a version</param>
      /// <returns>A <c>BibleVersion</c></returns>
      public static BibleVersion Version(string fileName)
      {
         return m_loadedBibles[fileName];
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


      /// <summary>
      /// Get a chapter's text
      /// </summary>
      /// <param name="xDocument"></param>
      /// <param name="book"></param>
      /// <param name="chapter"></param>
      /// <returns></returns>
      //public static XNode GetBookChapter(XDocument xDocument, BibleReference reference)
      //{
      //   return  .DescendantNodes().ElementAt(reference.chap);
      //}

      #endregion


      #region Tests

      public static String TestInformation()
      {
         String info = null;

         foreach(KeyValuePair<string, BibleVersion> bible in m_loadedBibles)
         {
            info = bible.Value.Language + ", " + bible.Value.VersionAbbreviation + ", " + bible.Value.VersionName;
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

         foreach(KeyValuePair<string, BibleVersion> bible in m_loadedBibles)
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