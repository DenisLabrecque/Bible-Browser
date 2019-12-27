using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BibleBrowserUWP
{
   static class BibleSearch
   {
      public const int TOOMANYRESULTS = 150;

      #region Methods

      /// <summary>
      /// Return the Bible book name that most resembles the query according to Levenshtein distance (ignoring diacritics).
      /// </summary>
      /// <param name="version">The version of the Bible in which to search through the book names</param>
      /// <param name="query">The text the user entered to match one of the Bible's book names</param>
      /// <returns>The closest book name to the query string. Guaranteed to return something, no matter how unreasonable.</returns>
      public static string ClosestBookName(BibleVersion version, string query, out float levensteinPercent)
      {
         if (query == null || query.Length == 0)
         {
            levensteinPercent = 0.0f;
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
                  levensteinPercent = 1.0f;
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
      /// Asynchronously search the Bible for every verse that contains a certain text as a substring.
      /// </summary>
      /// <param name="query">The string to be matched in the Bible reference for the verse to be returned.</param>
      public static async Task<SearchProgressInfo> SearchAsync(BibleVersion version, string query, IProgress<SearchProgressInfo> progress, CancellationToken cancellation)
      {
         SearchProgressInfo progressInfo = new SearchProgressInfo(query);
         query = query.ToLower().RemoveDiacritics();

         progressInfo = await Task.Run<SearchProgressInfo>(() =>
         {
            // Go through each book of the Bible
            for (int book = 0; book < version.BookNumbers.Count; book++)
            {
               BibleReference reference = new BibleReference(version, null, (BibleBook)book);
               progressInfo.Completion = DGL.Math.Percent(book + 1, version.BookNumbers.Count);
               progressInfo.Status = version.BookNames[book];
               progress.Report(progressInfo);

               // Go through each chapter of the book of the Bible
               for (int chapter = 1; chapter <= version.GetChapterCount(reference); chapter++)
               {
                  // See if the search has hit too many results for the computer's good
                  if (progressInfo.ResultCount > TOOMANYRESULTS)
                  {
                     progressInfo.Status = "Too many results.";
                     progressInfo.Completion = 1f;
                     progress.Report(progressInfo);
                  }
                  else
                  {
                     // Continue if the search was not cancelled
                     try
                     {
                        // Skip if the search was cancelled
                        cancellation.ThrowIfCancellationRequested();
                        BibleReference chapterReference = new BibleReference(version, null, (BibleBook)book, chapter);

                        // Go through each verse of the chapter
                        int verseNumber = 1;
                        foreach (string verse in version.GetChapterVerses(chapterReference))
                        {
                           if (verse.ToLower().RemoveDiacritics().Contains(query))
                           {
                              BibleReference hit = new BibleReference(version, null, (BibleBook)book, chapter, verseNumber);
                              Debug.WriteLine(hit + ":" + verseNumber + " -- " + verse);
                              progressInfo.AddResult(new SearchResult(hit, verse));
                           }

                           verseNumber++;
                        }
                     }
                     // Handle the search being cancelled
                     catch (OperationCanceledException)
                     {
                        progressInfo.IsCanceled = true; // Let the user know that the operation has been ended unexpectedly
                        return progressInfo;
                     }
                  }
               }
            }

            return progressInfo;
         });

         progressInfo.Status = "Search complete.";
         return progressInfo;
      }


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


      /// <summary>
      /// Calculate the Levenstein similarity probability that the query has a Bible book based on the version.
      /// </summary>
      /// <param name="splitQuery">The simplified query from which the version has been removed.</param>
      /// <param name="version">The version to find the Bible book in.</param>
      /// <param name="book">The closest book that was found. May be completely incorrect (check the return value)</param>
      /// <returns>The Levenstein distance of the closest Bible book to the result book string found.</returns>
      public static float QueryHasBibleBook(ref List<string> splitQuery, BibleVersion version, ref string book)
      {
         float levSimilarity;

         foreach(var item in splitQuery)
         {
            Debug.WriteLine("ITEM: " + item);
         }

         if(splitQuery.Count == 0)
         {
            return 0f;
         }
         else if (QueryHasNumeral(ref splitQuery)) {
            book = ClosestBookName(version, splitQuery[0], out levSimilarity);
            Debug.WriteLine(book + " found from " + splitQuery[0] + " with lev distance of " + levSimilarity);
            splitQuery.RemoveAt(0);
            return levSimilarity;
         }
         else
         {
            book = ClosestBookName(version, splitQuery[0], out levSimilarity);
            Debug.WriteLine(book + " found from " + splitQuery[0] + " with lev distance of " + levSimilarity);
            splitQuery.RemoveAt(0);
            return levSimilarity;
         }
      }


      /// <summary>
      /// Check whether a query has a Bible version.
      /// </summary>
      /// <param name="splitQuery">The original search query split between spaces and punctuation.
      /// Removes the BibleVersion parameter if it is found.</param>
      /// <param name="version">Changes to the correct BibleVersion if the version is found.</param>
      /// <returns>True if a version that is installed is found.</returns>
      public static bool QueryHasBibleVersion(ref List<string> splitQuery, ref BibleVersion version, ref BibleVersion compareVersion)
      {
         if (splitQuery.Count > 0 && VersionByAbbreviation(splitQuery[0]) != null)
         {
            if (splitQuery.Count > 1 && VersionByAbbreviation(splitQuery[1]) != null)
            {
               version = VersionByAbbreviation(splitQuery[0]);
               compareVersion = VersionByAbbreviation(splitQuery[1]);
               splitQuery.RemoveAt(0);
               splitQuery.RemoveAt(0);
               return true;
            }

            version = VersionByAbbreviation(splitQuery[0]);
            compareVersion = null;
            splitQuery.RemoveAt(0);
            return true;
         }
         else
            return false;
      }


      /// <summary>
      /// Find whether the query has a number or roman numeral from 1-10 as first item, with a  book name.
      /// If there is, the search query item will be removed from the list, and the chapter number will be assigned that value.
      /// </summary>
      /// <param name="query">The user's query that may start with a number.
      /// The first element will be removed if it is a number, and added to the book name.</param>
      /// <param name="chapter">Assigned if a number is found</param>
      /// <returns>Whether or not a number/numeral is found</returns>
      public static bool QueryHasNumeral(ref List<string> query)
      {
         // One space for the numeral, and one for the book name itself
         if(query.Count < 2)
         {
            return false;
         }
         else
         {
            int number = 0;

            // There is indeed a book number
            if (int.TryParse(query[0], out number))
            {
               query[1] = number + " " + query[1];
               query.RemoveAt(0);
               Debug.WriteLine("Query has numeral. " + query[0]);
               return true;
            }
            // The chapter number is a Roman numeral from i-x
            else if(IsRoman(query[0], ref number))
            {
               query[1] = number + " " + query[1];
               query.RemoveAt(0);
               Debug.WriteLine("Query has numeral. " + query[0]);
               return true;
            }
            // Not a number
            else
            {
               return false;
            }
         }
      }


      /// <summary>
      /// Finds whether the first item in the query is a number. If so, that number gets assigned to the chapter number.
      /// Otherwise, the chapter number is set as 1.
      /// </summary>
      /// <param name="splitQuery">The query, beginning with the chapter number.</param>
      /// <param name="chapter">The chapter number found</param>
      /// <returns>True if a chapter number was found.</returns>
      public static bool QueryHasChapter(ref List<string> splitQuery, ref int chapter)
      {
         foreach(var item in splitQuery)
         {
            Debug.WriteLine("QUERY: " + item);
         }

         if(splitQuery.Count == 0)
         {
            Debug.WriteLine("No chapter found.");
            return false;
         }
         else
         {
            int number = 1;

            if(int.TryParse(splitQuery[0], out number))
            {
               Debug.WriteLine("Parsed chapter number " + number);
               chapter = number;
               splitQuery.RemoveAt(0);
               return true;
            }
            else
            {
               Debug.WriteLine("Did not parse a chapter number");
               chapter = 1;
               splitQuery.RemoveAt(0);
               return false;
            }
         }
      }


      /// <summary>
      /// Simple check for whether the string of characters matches Roman numerals from 1-10 (i-x).
      /// Will NOT match numbers beyond this range.
      /// </summary>
      /// <param name="characters">The possible Roman numeral</param>
      /// <param name="number">The amount from 1-10 the numeral stands for</param>
      /// <returns>True if this is a Roman numeral from i to x</returns>
      public static bool IsRoman(string characters, ref int number)
      {
         switch(characters.ToLower())
         {
            case "i":
               number = 1;
               return true;
            case "ii":
               number = 2;
               return true;
            case "iii":
               number = 3;
               return true;
            case "iv":
               number = 4;
               return true;
            case "v":
               number = 5;
               return true;
            case "vi":
               number = 6;
               return true;
            case "vii":
               number = 7;
               return true;
            case "viii":
               number = 8;
               return true;
            case "ix":
               number = 9;
               return true;
            case "x":
               number = 10;
               return true;
            default:
               return false;
         }
      }

      public static bool QuerySurroundedByQuotes(ref List<string> splitQuery)
      {
         if (splitQuery.Count >= 1)
         {
            if ((splitQuery.First().First() == '"' && splitQuery.Last().Last() == '"') ||
                (splitQuery.First().First() == '\'' && splitQuery.Last().Last() == '\''))
            {
               return true;
            }
            else
               return false;
         }
         else
            return false;
      }

      public static string ReassembleSplitString(List<string> splitQuery, bool removeQuotes)
      {
         string query = string.Empty;

         foreach(string item in splitQuery)
         {
            query += item + " ";
         }

         if (removeQuotes)
         {
            char[] charsToTrim = { '\'', '"', ' ' };
            query = query.Trim(charsToTrim);
         }
         return query;
      }

      #endregion
   }
}
