using System;
using Windows.UI.Xaml;

namespace BibleBrowserUWP
{
   /// <summary>
   /// Verse text in the Bible.
   /// </summary>
   class Verse
   {
      #region Members

      private int m_number;
      private string m_text = string.Empty;
      private string m_compare = string.Empty;

      #endregion

      #region Properties

      public string MainText {
         get { return m_text; }
      }

      public string SecondText {
         get { return m_compare; }
      }

      public int Number {
         get { return m_number; }
      }

      public GridLength CompareWidth {
         get {
            if (string.IsNullOrEmpty(m_compare))
               return new GridLength(0);
            else
               return new GridLength(1, GridUnitType.Star);
         }
      }

      public GridLength ColumnWidth {
         get {
            if (string.IsNullOrEmpty(m_compare))
               return new GridLength(0);
            else
               return new GridLength(24);
         }
      }

      #endregion


      #region Constructors

      public Verse(string text, int number = 0)
      {
         if (text != null)
            m_text = text;
         m_number = number;
      }

      public Verse(string text, string compare, int number = 0)
      {
         if (text != null)
            m_text = text;
         if (compare != null)
            m_compare = compare;
         m_number = number;
      }

      #endregion

      /// <summary>
      /// Overridden string method.
      /// </summary>
      /// <returns>This verse in plain text format.</returns>
      public override string ToString()
      {
         return m_text;
      }
   }
}
