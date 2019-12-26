using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BibleBrowserUWP
{
   /// <summary>
   /// Hold some metadata about a search (degree of completion, current task, etc.)
   /// Notifies that properties change.
   /// </summary>
   public class SearchProgressInfo
   {
      private float m_Progress;
      private string m_Task;
      private List<SearchResult> m_Results;
      private string m_Query;
      private DateTime m_TimeStarted;
      private DateTime m_TimeEnded;
      private bool m_IsCanceled;
      private int m_ResultCount;


      /// <summary>
      /// Constructor that initializes the new search.
      /// </summary>
      public SearchProgressInfo(string query)
      {
         m_Query = query;
         m_Progress = 0f;
         m_ResultCount = 0;
         m_Task = string.Empty;
         m_Results = new List<SearchResult>();
         m_TimeStarted = DateTime.Now;
         m_TimeEnded = DateTime.Now;
         m_IsCanceled = false;
      }

      /// <summary>
      /// Percent of completion from 0 to 1. Updates the search time.
      /// </summary>
      public float Completion {
         get {
            return m_Progress;
         }
         set {
            m_Progress = value;
            m_TimeEnded = DateTime.Now;
         }
      }

      /// <summary>
      /// The amount of time spent searching until the search progress was marked complete.
      /// </summary>
      public TimeSpan SearchTime {
         get {
            return m_TimeEnded - m_TimeStarted;
         }
      }

      /// <summary>
      /// The search query that initialized this search.
      /// </summary>
      public string Query {
         get {
            return m_Query;
         }
      }

      /// <summary>
      /// Short description of the work currently being done.
      /// </summary>
      public string Status {
         get {
            return m_Task;
         }
         set {
            m_Task = value;
         }
      }

      /// <summary>
      /// The list of results found, updated dynamically as results are added.
      /// </summary>
      public List<SearchResult> Results {
         get { return m_Results; }
      }

      /// <summary>
      /// Main method to add a search result to the list of search hits.
      /// </summary>
      /// <param name="match">A reference that matches the search query being run.</param>
      public void AddResult(SearchResult match)
      {
         m_Results.Add(match);
         m_ResultCount++;
      }

      /// <summary>
      /// Whether this operation has been ended by the user.
      /// </summary>
      public bool IsCanceled {
         get { return m_IsCanceled; }
         set { m_IsCanceled = value; }
      }

      /// <summary>
      /// The number of matches that have been added to the list to date.
      /// </summary>
      public int ResultCount {
         get { return m_ResultCount; }
      }
   }
}
