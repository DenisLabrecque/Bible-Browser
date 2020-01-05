using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BibleBrowserUWP
{
   /// <summary>
   /// Represents a search that a user has fired.
   /// </summary>
   public class SearchItem
   {
      private string m_rawQuery = null;
      SearchProgressInfo m_progress = null;

      public Progress<SearchProgressInfo> Progress { get; set; }

      /// <summary>
      /// This needs to be done during search.
      /// If not set, the search will mark as being incomplete.
      /// </summary>
      public SearchProgressInfo SearchProgressInfo {
         get { return m_progress; } 
         set {
            if (value == null)
               throw new ArgumentNullException();
            else
               m_progress = value;
         }
      }

      /// <summary>
      /// Constructor.
      /// </summary>
      /// <param name="rawQuery">The search query the user typed, without alteration.</param>
      public SearchItem(string rawQuery)
      {
         if (string.IsNullOrEmpty(rawQuery))
            throw new ArgumentNullException("A raw query cannot be null or empty");
         else
            m_rawQuery = rawQuery;
      }

      /// <summary>
      /// The query entered by the user. May return  null.
      /// </summary>
      public string RawQuery {
         get { return m_rawQuery; }
      }

      /// <summary>
      /// Whether the search has successfully ended with all possible results being found.
      /// </summary>
      public bool IsComplete {
         get {
            if (m_progress == null)
            {
               Debug.WriteLine("Progress is null");
               return false;
            }
            else
               return m_progress.IsComplete;
         }
      }
   }
}
