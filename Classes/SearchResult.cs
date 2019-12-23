using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BibleBrowserUWP
{
   public class SearchResult
   {
      public BibleReference Reference { get; set; }
      public string Text { get; set; }

      public string FullReference {
         get { return Reference.FullReference; }
      }

      public string VerseText {
         get {
            return Text;
         }
      }

      /// <summary>
      /// Constructor.
      /// </summary>
      public SearchResult(BibleReference reference, string verse)
      {
         if(reference == null)
         {
            throw new ArgumentNullException("The reference passed was null");
         }
         else
         {
            Reference = reference;
         }

         if(verse == null)
         {
            throw new ArgumentNullException("The verse text passed was null");
         }
         else
         {
            Text = verse;
         }
      }
   }
}
