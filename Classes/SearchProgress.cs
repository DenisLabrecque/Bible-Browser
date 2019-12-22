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
   public class SearchProgressInfo : INotifyPropertyChanged
   {
      private static SearchProgressInfo m_StaticProgress = null; // Reference back to a single progress object

      private float m_Progress;
      private string m_Task;
      private ObservableCollection<BibleReference> m_Results;
      private string m_Query;
      private DateTime m_TimeStarted;
      private DateTime m_TimeEnded;

      public event PropertyChangedEventHandler PropertyChanged;

      /// <summary>
      /// Call this to reset the search before calling it. This removes any previous search result.
      /// Sets the search start time.
      /// </summary>
      public void StartNewSearch(string query)
      {
         m_Query = query;
         Reinitialize();
         m_TimeStarted = DateTime.Now;
         m_TimeEnded = DateTime.Now;
      }

      /// <summary>
      /// Reinitializes values like a constructor.
      /// </summary>
      private void Reinitialize()
      {
         m_Progress = 0f;
         m_Task = "Doing Nothing";
         if(m_Results == null)
         {
            m_Results = new ObservableCollection<BibleReference>();
         }
         else
         {
            m_Results.Clear();
         }
      }

      // This method is called by the Set accessor of each property.  
      // The CallerMemberName attribute that is applied to the optional propertyName  
      // parameter causes the property name of the caller to be substituted as an argument.  
      private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      /// <summary>
      /// Percent of completion from 0 to 1. Updates the search time.
      /// </summary>
      public float Progress {
         get {
            return m_Progress;
         }
         set {
            m_Progress = value;
            m_TimeEnded = DateTime.Now;
            NotifyPropertyChanged();
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
      public string Task {
         get {
            return m_Task;
         }
         set {
            m_Task = value;
            NotifyPropertyChanged();
         }
      }

      /// <summary>
      /// The list of results found, updated dynamically as results are added.
      /// </summary>
      public ObservableCollection<BibleReference> Results {
         get { return m_Results; }
      }

      /// <summary>
      /// A single reference to one instance of this class.
      /// </summary>
      public static SearchProgressInfo Single {
         get {
            if(m_StaticProgress == null)
            {
               m_StaticProgress = new SearchProgressInfo();
            }

            return m_StaticProgress;
         }
         set { m_StaticProgress = value; }
      }

      /// <summary>
      /// Main method to add a search result to the list of search hits.
      /// </summary>
      /// <param name="match">A reference that matches the search query being run.</param>
      public void AddResult(BibleReference match)
      {
         m_Results.Add(match);
         NotifyPropertyChanged("SearchResults");
      }
   }
}
