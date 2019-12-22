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
   public class SearchProgress : INotifyPropertyChanged
   {
      private static SearchProgress m_StaticProgress = null; // Reference back to a single progress object

      private float m_Progress;
      private string m_Task;
      private ObservableCollection<BibleReference> m_Results = new ObservableCollection<BibleReference>();

      public event PropertyChangedEventHandler PropertyChanged;

      /// <summary>
      /// Reinitializes values like a constructor.
      /// </summary>
      public void Reinitialize()
      {
         m_Progress = 0f;
         m_Task = "Doing Nothing";
         m_Results = new ObservableCollection<BibleReference>();
      }

      // This method is called by the Set accessor of each property.  
      // The CallerMemberName attribute that is applied to the optional propertyName  
      // parameter causes the property name of the caller to be substituted as an argument.  
      private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      /// <summary>
      /// Percent of completion from 0 to 1.
      /// </summary>
      public float Progress {
         get {
            return m_Progress;
         }
         set {
            m_Progress = value;
            NotifyPropertyChanged();
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
      public static SearchProgress StaticProgress {
         get {
            if(m_StaticProgress == null)
            {
               m_StaticProgress = new SearchProgress();
            }

            Debug.WriteLine("/n/n................................................");
            Debug.WriteLine("STATIC PROGRESS RETURNED/n/n");
            return m_StaticProgress;
         }
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
