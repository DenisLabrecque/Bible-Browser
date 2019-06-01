using System;

namespace BibleBrowser
{
   enum BookShortName { Gn, Ex, Lv, Nb, Dt, Jos, Jg, Rt, iS, iiS, iK, iiK, iCh, iiCh, Esr, Ne, Est, Jb, Ps, Pr, Ecc, Sng, Is, Jr, Lm, Ez, Dn, Hos, Jl, Am, Ob, Jon, Mi, Na, Ha, Zep, Hag, Za, Mal, Mt, Mc, Lc, Jn, Ac, Rm, iCo, iiCo, Ga, Ep, Ph, Col, iTh, iiTh, iTm, iiTm, Tt, Phm, He, Jc, iP, iiP, iJn, iiJn, iiiJn, Jude, Rev }

   /// <summary>
   /// Describes a Bible reference with book name, chapter, and verse.
   /// </summary>
   internal class BibleReference
   {
      #region Constants
      public const int MAX_REFERENCE_WORDS = 10;
      #endregion

      #region Properties
      public int? Book { get; private set; }
      public string BookName { get; private set; }
      public int BookNumeral { get; private set; }
      public ChapterVerse Start { get; private set; }
      public ChapterVerse End { get; private set; }
      #endregion

      #region Fields
      public static char[] WordSeparators = {
         ' ',
         '*'
      };
      #endregion

      #region Constructor
      /// <summary>
      /// Basic constructor.
      /// </summary>
      public BibleReference(int bookNumeral, int book, ChapterVerse start, ChapterVerse end)
      {
         Initialize(bookNumeral, start, end, book);
      }

      public BibleReference(int bookNumeral, string bookName, ChapterVerse start, ChapterVerse end)
      {
         Initialize(bookNumeral, start, end, null, bookName);
      }
      #endregion

      #region Public Methods
      /// <summary>
      /// Change a reference once it has been set using a book index
      /// </summary>
      public void ChangeReference(int bookNumeral, int book, ChapterVerse start, ChapterVerse end)
      {
         Initialize(bookNumeral, start, end, book);
      }

      /// <summary>
      /// Change a reference once it has been set using a book name
      /// </summary>
      public void ChangeReference(int bookNumeral, string bookName, ChapterVerse start, ChapterVerse end)
      {
         Initialize(bookNumeral, start, end, null, bookName);
      }

      /// <summary>
      /// If a string contains a number and letters, return true; if not, return false.
      /// </summary>
      /// <param name="reference">The user's reference lookup string</param>
      /// <returns></returns>
      public static bool MaybeReference(string reference)
      {
         bool containsNumber = false;
         bool containsLetter = false;

         foreach(char character in reference)
         {
            if(char.IsLetter(character))
            {
               containsLetter = true;
            }
            else if(char.IsDigit(character))
            {
               containsNumber = true;
            }
         }

         return containsLetter && containsNumber;
      }

      /// <summary>
      /// Cast a string to a Bible reference.
      /// </summary>
      /// <param name="reference">The original string</param>
      /// <returns>A reference if the string can be parsed, or null if not</returns>
      public static BibleReference ToReference(string reference)
      {
         string[] splitUpReference;

         int bookPrefix = 0;
         string bookName = null;
         ChapterVerse startChapter = null;
         ChapterVerse endChapter = null;

         splitUpReference = SplitUpReference(reference);

         // Got through each word or number group in a Bible reference
         for(int i = 0; i < splitUpReference.Length; i++)
         {
            // When the first word is a number, it denotes one of multiple books (eg. 1 John)
            // The array element after this will be the book name
            try
            {
               bookPrefix = int.Parse(splitUpReference[0]);
            }
            catch
            {
               bookPrefix = 0;
            }

            // Get the first element that can be a book name
            if(bookName == null && !ChapterVerse.IsChapterVerse(splitUpReference[i]))
            {
               bookName = splitUpReference[i];
            }

            // Get the first element that can be a chapter and verse
            else if(startChapter == null)
            {
               startChapter = new ChapterVerse(splitUpReference[i]);
            }
            else if(endChapter == null)
            {
               endChapter = new ChapterVerse(splitUpReference[i]);

               // Stop the loop at this point
               break;
            }
         }

         // Return a reference if it has been successfully parsed out
         if(bookName != null)
         {
            return new BibleReference(0, bookName, new ChapterVerse(), new ChapterVerse());
         }
         else
         {
            return null;
         }
      }

      /// <summary>
      /// Separate words and numbers that are stuck together
      /// </summary>
      /// <param name="lookupString">The user's reference search string</param>
      /// <returns>An array of strings representing words and number groups</returns>
      public static string[] SplitUpReference(string lookupString)
      {
         lookupString = SpaceOutNumbers(lookupString, "*");
         return lookupString.Split(WordSeparators, MAX_REFERENCE_WORDS, StringSplitOptions.RemoveEmptyEntries);
      }
      #endregion

      #region Private Methods
      /// <summary>
      /// Method used to initialize or change references.
      /// </summary>
      private void Initialize(int bookNumeral, ChapterVerse start, ChapterVerse end, int? book = null, string bookName = null)
      {
         if(bookNumeral >= 0)
         {
            BookNumeral = bookNumeral;
         }
         else
         {
            throw new Exception("A book numeral must be greater than 1, or zero if nonexistent");
         }

         if(book != null && book < (int)BookShortName.Gn || book > (int)BookShortName.Rev)
         {
            throw new Exception("A reference must be from the book short name enumeration");
         }
         else
         {
            Book = book;
         }

         if(bookName != null && bookName.Length <= 1)
         {
            throw new Exception("A book name should be at least two characters long");
         }
         else
         {
            BookName = bookName;
         }

         Start = start;
         End = end;
         if(End.Chapter > 0 && End < Start)
         {
            throw new Exception("A start reference cannot begin at a point later than an end reference");
         }
      }

      /// <summary>
      /// Add spaces between numbers and letters in a string.
      /// </summary>
      /// <param name="spaceThis">The string which has numbers stuck to other characters</param>
      /// <param name="separator">The character used to separate numbers. Must not be a digit.</param>
      /// <returns></returns>
      private static string SpaceOutNumbers(string spaceThis, string separator)
      {
         string spacedString = spaceThis;

         // Go through each character in a string
         for(int i = 0, j = 0; i < spaceThis.Length; i++, j++)
         {
            try
            {
               // Replace <digit><letter> with <digit> <letter>
               if(char.IsDigit(spaceThis[i]) && char.IsLetter(spaceThis[i + 1]))
               {
                  j++;
                  spacedString = spacedString.Insert(j, separator);
               }
               // Replace <letter><digit> with <letter> <digit>
               else if(char.IsLetter(spaceThis[i]) && char.IsDigit(spaceThis[i + 1]))
               {
                  j++;
                  spacedString = spacedString.Insert(j, separator);
               }
            }
            catch
            {
               // Ignore out of range exceptions
            }
         }

         return spacedString;
      }

      /// <summary>
      /// Returns the number of integers an array contains.
      /// This helps know whether a reference contains many chapters and verses. Eg. 2 Chronicles 2:19-3:15 would contain 5 integers.
      /// </summary>
      private static int IntsInArray(string[] array)
      {
         int numberOfInts = 0;
         int temporaryResult;

         foreach(string substring in array)
         {
            if(int.TryParse(substring, out temporaryResult))
            {
               numberOfInts++;
            }
         }

         return numberOfInts;
      }
      #endregion

      #region Overrides
      /// <summary>
      /// Overridden string method.
      /// </summary>
      /// <returns>A typographically correct biblical reference.</returns>
      public override string ToString()
      {
         string reference;

         // Add the book numeral and name
         if(BookNumeral > 0)
         {
            reference = BookNumeral + " " + Enum.GetName(typeof(BookShortName), Book);
         }
         else
         {
            reference = Enum.GetName(typeof(BookShortName), Book);
         }

         // Add the chapter
         if(Start.Chapter > 0)
         {
            reference += " " + Start;

            // Add the end chapter
            if(End.Chapter > 0 && End.Verse > Start.Verse)
            {
               reference += "–" + End;
            }
         }

         return reference;
      }
      #endregion
   }
}