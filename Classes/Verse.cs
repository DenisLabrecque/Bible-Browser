using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BibleBrowserUWP
{
   /// <summary>
   /// Verse text in the Bible.
   /// </summary>
   class Verse : INotifyPropertyChanged
   {
      #region Property Changed Implementation

      public event PropertyChangedEventHandler PropertyChanged;

      // This method is called by the Set accessor of each property.  
      // The CallerMemberName attribute that is applied to the optional propertyName  
      // parameter causes the property name of the caller to be substituted as an argument.  
      private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      #endregion


      #region Properties

      public string Text { get {
            if (Text == null)
               return string.Empty;
            else
               return Text;
         }
         set {
            Text = value;
         }
      }
      public string ComparisonText {
         get {
            if (ComparisonText == null)
               return string.Empty;
            else
               return ComparisonText;
         }
         set {
            ComparisonText = value;
         }
      }

      #endregion


      #region Constructors

      public Verse(string plainText, string comparisonText = null)
      {
         Text = plainText;
         ComparisonText = comparisonText;
      }

      #endregion

      /// <summary>
      /// Overridden string method.
      /// </summary>
      /// <returns>This verse in plain text format.</returns>
      public override string ToString()
      {
         return Text;
      }
   }
}
