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
   static class BibleSearch
   {
      #region Methods

      /// <summary>
      /// Calculate percentage similarity of two strings
      /// <param name="source">Source String to Compare with</param>
      /// <param name="target">Targeted String to Compare</param>
      /// <returns>Return Similarity between two strings from 0 to 1.0</returns>
      /// </summary>
      public static float LevenshteinSimilarity(string source, string target)
      {
         if ((source == null) || (target == null)) return 0.0f;
         else if ((source.Length == 0) || (target.Length == 0)) return 0.0f;
         else if (source == target) return 1.0f;

         int stepsToSame = ComputeLevenshteinDistance(source, target);
         return (1.0f - ((float)stepsToSame / (float)Math.Max(source.Length, target.Length)));
      }


      /// <summary>
      /// Return the Bible book name that most resembles the query according to Levenshtein distance (ignoring diacritics).
      /// </summary>
      /// <param name="version">The version of the Bible in which to search through the book names</param>
      /// <param name="query">The text the user entered to match one of the Bible's book names</param>
      /// <returns>The closest book name to the query string. Guaranteed to return something, no matter how unreasonable.</returns>
      public static string ClosestBookName(BibleVersion version, string query, out float levensteinPercent)
      {
         Debug.WriteLine("NEW QUERY: " + query.ToLower().RemoveDiacritics());
         if (query == null || query.Length == 0)
         {
            levensteinPercent = 0f;
            return version.BookNames.First();
         }

         // When the user types an exact match to the first letters of a book, return that book.
         // For example, EPH is EPHesians, not Esther as according to the Levenshtein distance.
         foreach (string bookName in version.BookNames)
         {
            // Include only queries that are a subset of the book name
            if (query.Length <= bookName.Length)
            {
               if (query.ToLower().RemoveDiacritics() == bookName.Substring(0, query.Length).ToLower().RemoveDiacritics())
               {
                  levensteinPercent = 1f;
                  return bookName;
               }
            }
         }

         // Store the percent similarity for each book name
         SortedDictionary<float, string> results = new SortedDictionary<float, string>();

         // With this, a book name is guaranteed to be returned, so unintentional misspellings can be ignored.
         // However, because typing ASDF will return AMOS, it is best to check the Levenstein distance for a too large error margin,
         // so that a better search result can be presented instead.
         float similarity = 0f;
         foreach (string bookName in version.BookNames)
         {
            similarity = LevenshteinSimilarity(query.ToLower().RemoveDiacritics(), bookName.ToLower().RemoveDiacritics());
            if (similarity == 1.0f)
            {
               levensteinPercent = similarity;
               return bookName;
            }
            else
               results.TryAdd(similarity, bookName);
         }

         Debug.WriteLine("FOUND " + results.Last().Value + " WITH A SIMILARITY OF " + similarity);
         levensteinPercent = similarity;
         return results.Last().Value;
      }


      /// <summary>
      /// Return a <c>BibleVersion</c> if the query matches a bible version abbreviation exactly.
      /// Return <c>null</c> if not.
      /// </summary>
      /// <param name="query">The text that is a version abbreviation (eg. "KJV")</param>
      public static BibleVersion VersionByAbbreviation(string query)
      {
         foreach (BibleVersion version in BibleLoader.Bibles)
         {
            if (query.ToLower() == version.Abbreviation.ToLower())
            {
               return version;
            }
         }

         return null;
      }


      /// <summary>
      /// Search the Bible for every verse that contains a certain text as a substring.
      /// </summary>
      /// <param name="query">The string to be matched in the Bible reference for the verse to be returned.</param>
      public static void Search(BibleVersion version, string query)
      {
         query = query.ToLower().RemoveDiacritics();
         SearchProgress.Single.Reinitialize();

         // Go through each book of the Bible
         for (int book = 0; book < version.BookNumbers.Count; book++)
         {
            BibleReference reference = new BibleReference(version, null, (BibleBook)book);
            SearchProgress.Single.Progress = DGL.Math.Percent(book + 1, version.BookNumbers.Count);
            SearchProgress.Single.Task = "Searching " + version.BookNames[book];

            Debug.WriteLine("-----------------------------------------------------");
            Debug.WriteLine("Progress " + SearchProgress.Single.Progress + "%");
            Debug.WriteLine(SearchProgress.Single.Task);
            Debug.WriteLine("-----------------------------------------------------");

            // Go through each chapter of the book of the Bible
            for (int chapter = 1; chapter <= version.GetChapterCount(reference); chapter++)
            {
               BibleReference chapterReference = new BibleReference(version, null, (BibleBook)book, chapter);

               // Go through each verse of the chapter
               int verseNumber = 1;
               foreach (string verse in version.GetChapterVerses(chapterReference))
               {
                  if (verse.ToLower().RemoveDiacritics().Contains(query))
                  {
                     BibleReference hit = new BibleReference(version, null, (BibleBook)book, chapter, verseNumber);
                     Debug.WriteLine(hit + ":" + verseNumber + " -- " + verse);
                     SearchProgress.Single.AddResult(hit);
                  }

                  verseNumber++;
               }
            }

            SearchProgress.Single.Task = "Done.";
         }
      }


      /// <summary>
      /// Returns the number of steps required to transform a source string
      /// into a target string.
      /// </summary>
      private static int ComputeLevenshteinDistance(string source, string target)
      {
         if ((source == null) || (target == null)) return 0;
         if ((source.Length == 0) || (target.Length == 0)) return 0;
         if (source == target) return source.Length;

         int sourceWordCount = source.Length;
         int targetWordCount = target.Length;

         // Step 1
         if (sourceWordCount == 0)
            return targetWordCount;

         if (targetWordCount == 0)
            return sourceWordCount;

         int[,] distance = new int[sourceWordCount + 1, targetWordCount + 1];

         // Step 2
         for (int i = 0; i <= sourceWordCount; distance[i, 0] = i++) ;
         for (int j = 0; j <= targetWordCount; distance[0, j] = j++) ;

         for (int i = 1; i <= sourceWordCount; i++)
         {
            for (int j = 1; j <= targetWordCount; j++)
            {
               // Step 3
               int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;

               // Step 4
               distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1), distance[i - 1, j - 1] + cost);
            }
         }

         return distance[sourceWordCount, targetWordCount];
      }

      #endregion
   }
}
