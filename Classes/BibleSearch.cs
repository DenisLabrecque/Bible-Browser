﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BibleBrowser
{
   static class BibleSearch
   {
      #region Members



      #endregion



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
      /// Return the Bible book name that most resembles the query according to Levenshtein distance.
      /// </summary>
      public static string ClosestBookName(BibleVersion version, string query)
      {
         // Store the percent similarity for each book name
         SortedDictionary<float, string> results = new SortedDictionary<float, string>();

         foreach (string bookName in version.BookNames)
         {
            float similarity = LevenshteinSimilarity(query.ToLower(), bookName.ToLower());
            if (similarity == 1.0f)
               return bookName;
            else
               results.TryAdd(similarity, bookName);
         }
            

         return results.Last().Value;
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
