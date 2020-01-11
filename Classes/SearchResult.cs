using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Documents;

namespace BibleBrowserUWP
{
   public class SearchResult : IComparable
   {
      string m_highlightText = string.Empty;

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

      public string DisplayText {
         get {
            return Text;
         }
      }

      public string HighlightText { get { return m_highlightText; }
         set {
            if (string.IsNullOrWhiteSpace(VerseText))
               throw new ArgumentException("Highlight text cannot be set before the verse text is known");
            else
            {
               string highlight = value.ToLower().RemoveDiacritics();
               string verse = VerseText.ToLower().RemoveDiacritics();
               int startIndex = verse.IndexOf(highlight);
               string substring = VerseText.Substring(startIndex, highlight.Length);

               if (string.IsNullOrWhiteSpace(substring))
                  m_highlightText = string.Empty;
               else
                  m_highlightText = substring;
            }
         }
      }

      /// <summary>
      /// Constructor.
      /// </summary>
      public SearchResult(BibleReference reference, string verse, string highlight)
      {
         if(reference == null)
            throw new ArgumentNullException("The reference passed was null");
         else
            Reference = reference;

         if(verse == null)
            throw new ArgumentNullException("The verse text passed was null");
         else
            Text = verse;

         HighlightText = highlight;
      }

      /// <summary>
      /// Used for sorting.
      /// </summary>
      public int CompareTo(object obj)
      {
         SearchResult other = obj as SearchResult;

         if (other == null)
            return 0;
         else
         {
            if (other.Reference.Book > Reference.Book)
               return -1;
            else if (other.Reference.Book < Reference.Book)
               return 1;
            else // Equal books
            {
               if (other.Reference.Chapter > Reference.Chapter)
                  return -1;
               else if (other.Reference.Chapter < Reference.Chapter)
                  return 1;
               else // Equal chapters
               {
                  if (other.Reference.Verse > Reference.Verse)
                     return -1;
                  else if (other.Reference.Verse < Reference.Verse)
                     return 1;
                  else // Equal verses;
                     return 0;
               }
            }
         }
      }
   }

   /// <summary>
   /// Extension methods to allow lists to be sorted.
   /// </summary>
   public static class ListExtension
   {
      public static void BubbleSort(this IList o)
      {
         for (int i = o.Count - 1; i >= 0; i--)
         {
            for (int j = 1; j <= i; j++)
            {
               object o1 = o[j - 1];
               object o2 = o[j];
               if (((IComparable)o1).CompareTo(o2) > 0)
               {
                  o.Remove(o1);
                  o.Insert(j, o1);
               }
            }
         }
      }
   }
}
