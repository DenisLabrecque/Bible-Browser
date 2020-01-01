using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BibleBrowserUWP
{
   /// <summary>
   /// A search query item in history.
   /// </summary>
   public class SearchItem
   {
      private string m_rawQuery = null;
      ObservableCollection<SearchResult> m_results = null;

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
      /// The search results caused by the query.
      /// </summary>
      public ObservableCollection<SearchResult> Results {
         get {
            return m_results;
         }
         set {
            if (value == null)
               throw new ArgumentNullException("The results need to be set to a value, and not null.");
            m_results = value;
         }
      }
   }
}
