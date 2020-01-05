using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BibleBrowserUWP
{
   /// <summary>
   /// Hold some metadata about a search (degree of completion, current task, etc.)
   /// </summary>
   public class SearchProgressInfo
   {
      private float m_Progress;
      private string m_status;
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
         m_status = string.Empty;
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
            if(m_Progress < 1f)
               return m_status;
            else
            {
               var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

               // Show the number of results as status
               if (m_ResultCount == 0)
               {
                  return loader.GetString("noResultFor") + " " + loader.GetString("quoteLeft") + m_Query + loader.GetString("quoteRight");
               }
               else if (m_Results.Count == 1)
               {
                  return loader.GetString("oneResultFor") + " " + loader.GetString("quoteLeft") + m_Query + loader.GetString("quoteRight");
               }
               else if(m_ResultCount > BibleSearch.TOOMANYRESULTS)
               {
                  return loader.GetString("tooManyResultsFor") + " " + loader.GetString("quoteLeft") + m_Query + loader.GetString("quoteRight");
               }
               {
                  return m_ResultCount + " " + loader.GetString("manyResultsFor") + " " + loader.GetString("quoteLeft") + m_Query + loader.GetString("quoteRight");
               }
            }
         }
         set {
            m_status = value;
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

      public bool IsComplete {
         get { Debug.WriteLine("Progress was::::::: " + m_Progress + (m_Progress >= 1.0f ? true : false)); return m_Progress >= 1.0f ? true : false; }
      }
   }
}
