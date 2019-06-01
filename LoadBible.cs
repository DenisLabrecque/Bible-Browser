using System;
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
   class ReadBible
   {
      static List<BibleMeta> loadedBibles = new List<BibleMeta>();

      public class Constant
      {
         public static String BIBLE_PATH = "Bibles";
         public static List<String> BIBLE_LOAD_LIST = new List<String>() { "LSG.xml", "LSGTesting.xml" };

         public class Zefania
         {
            public static String XML_BOOK_NODE = "BIBLEBOOK";
            public static String BOOK_NAME_ATTRIBUTE = "bname";
            public static String BOOK_SHORT_NAME_ATTRIBUTE = "bsname";
            public static String BOOK_NUMBER_ATTRIBUTE = "bnumber";
            public static String BIBLE_NAME_ATTRIBUTE = "biblename";
            public static String VERSION_ABBREVIATION = "identifier";
            public static String LANGUAGE_NODE = "language";
            public static String INFORMATION_NODE = "INFORMATION";
         }
      }

      /// <summary>
      /// A class able to create a metadata summary of an XML Bible document.
      /// </summary>
      public class BibleMeta
      {
         String language;
         String filePath;
         String versionName;
         String versionAbbreviation;

         List<String> bookNames = new List<String>();
         List<String> bookAbbreviations = new List<String>();
         List<int> bookNumbers = new List<int>();

         BibleReference presentReference;

         #region Constructors
         public BibleMeta(String fileName)
         {
            filePath = Path.Combine(Package.Current.InstalledLocation.Path, Constant.BIBLE_PATH + "/" + fileName);

            // Create the XML document object
            XDocument loadedData = XDocument.Load(filePath);

            // Set information from XML
            language = loadedData.Root.Element(Constant.Zefania.INFORMATION_NODE).Element(Constant.Zefania.LANGUAGE_NODE).Value;
            versionName = loadedData.Root.Attribute(Constant.Zefania.BIBLE_NAME_ATTRIBUTE).Value;
            versionAbbreviation = loadedData.Root.Element(Constant.Zefania.INFORMATION_NODE).Element(Constant.Zefania.VERSION_ABBREVIATION).Value;

            // Find the book names in XML
            foreach(XElement element in loadedData.Descendants(Constant.Zefania.XML_BOOK_NODE))
            {
               bookNames.Add(element.Attribute(Constant.Zefania.BOOK_NAME_ATTRIBUTE).Value);
            }

            // Find the book short names in XML
            foreach(XElement element in loadedData.Descendants(Constant.Zefania.XML_BOOK_NODE))
            {
               bookAbbreviations.Add(element.Attribute(Constant.Zefania.BOOK_SHORT_NAME_ATTRIBUTE).Value);
            }

            // Find the book numbers in XML
            foreach(XElement element in loadedData.Descendants(Constant.Zefania.XML_BOOK_NODE))
            {
               bookNumbers.Add(Int32.Parse(element.Attribute(Constant.Zefania.BOOK_NUMBER_ATTRIBUTE).Value));
            }
         }
         #endregion

         #region Getters
         public String GetLanguage()
         {
            return language;
         }

         public String GetVersionName()
         {
            return versionName;
         }

         public String GetVersionAbbreviation()
         {
            return versionAbbreviation;
         }

         public List<String> GetBookNames()
         {
            return bookNames;
         }

         public List<String> GetBookAbbreviations()
         {
            return bookAbbreviations;
         }

         public List<int> GetBookNumbers()
         {
            return bookNumbers;
         }

         #endregion

         #region Setters
         public void SetReference(string reference)
         {

         }
         #endregion

         #region Properties

         #endregion
      }

      public static void LoadBibles()
      {
         foreach(string fileName in Constant.BIBLE_LOAD_LIST)
         {
            loadedBibles.Add(new BibleMeta(fileName));
         }

      }

      // Ajithsz, How to get Tag name of Xml by using XDocument
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

      public static string GetBookChapter(XDocument xDocument, string book, string chapter)
      {
         XNode Chapter = GetElement(xDocument, book).DescendantNodes().ElementAt(int.Parse(chapter));

         return Chapter.ToString();
      }

      #region Tests
      public static String TestInformation()
      {
         String info = null;

         foreach(BibleMeta bible in loadedBibles)
         {
            info = bible.GetLanguage() + ", " + bible.GetVersionAbbreviation();
         }

         return info;
      }

      public static String TestBookNames()
      {
         String bookNames = null;

         foreach(BibleMeta bible in loadedBibles)
         {
            List<String> bknames = bible.GetBookNames();

            foreach(String name in bknames)
            {
               bookNames += name;
            }
         }

         return bookNames;
      }
      #endregion
   }
}
