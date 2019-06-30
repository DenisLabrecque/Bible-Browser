using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace BibleBrowser
{
   enum BibleBook { Gn, Ex, Lv, Nb, Dt, Jos, Jg, Rt, iS, iiS, iK, iiK, iCh, iiCh, Esr, Ne, Est, Jb, Ps, Pr, Ecc, Sng, Is, Jr, Lm, Ez, Dn, Hos, Jl, Am, Ob, Jon, Mi, Na, Ha, Zep, Hag, Za, Mal, Mt, Mc, Lc, Jn, Ac, Rm, iCo, iiCo, Ga, Ep, Ph, Col, iTh, iiTh, iTm, iiTm, Tt, Phm, He, Jc, iP, iiP, iJn, iiJn, iiiJn, Jude, Rev }

   enum BookNumeral { none, i, ii, iii }

   /// <summary>
   /// Describes a Bible reference with book name, chapter, and verse.
   /// </summary>
   internal class BibleReference
   {
      #region Members

      public static char[] WordSeparators = {
         ' ',
         '*'
      };

      #endregion


      #region Properties

      public BibleVersion Version { get; private set; }
      public BookNumeral Numeral { get; private set; }
      public BibleBook Book { get; private set; }
      public string SimplifiedReference { get => BookName + " " + Chapter; } // Book name and chapter

      /// <summary>
      /// The book name indexed in this Bible version.
      /// Set dynamically by retrieval using the <code>BibleBook</code> enumeration as index for the <code>BookNames</code> loaded list.
      /// </summary>
      public string BookName { get => Version.BookNames[(int)Book]; }
      public int Chapter { get; private set; }
      public int Verse { get; private set; }
      public List<int> Chapters {
         get {
            List<int> chapters = new List<int>();
            for(int i = 0; i < Version.GetChapterCount(this); i++)
            {
               chapters.Add(i + 1);
            }
            return chapters;
         }
      }

      #endregion


      #region Constructor

      /// <summary>
      /// Default constructor.
      /// If the chapter or verse are set too high, they will be clamped to the maximum chapter or verse.
      /// If the chapter or verse are set too low, an <c>ArgumentOutOfRangeException</c> is thrown.
      /// </summary>
      public BibleReference(BibleVersion version, BibleBook book = BibleBook.Gn, int chapter = 1, int verse = 1)
      {
         Version = version ?? throw new ArgumentNullException("A BibleReference cannot be created with a null BibleVersion");
         Book = book;

         if (chapter < 1)
            throw new ArgumentOutOfRangeException("A chapter cannot be set to less than 1");
         else if (chapter > Version.GetChapterCount(this))
            Chapter = Version.GetChapterCount(this);
         else
            Chapter = chapter;

         if (verse < 1)
            throw new ArgumentOutOfRangeException("A verse cannot be set to less than 1");
         else if (verse > Version.GetVerseCount(this))
            Verse = Version.GetVerseCount(this);
         else
            Verse = verse;

         switch(book)
         {
            case BibleBook.iS:
               Numeral = BookNumeral.i;
               break;
            case BibleBook.iiS:
               Numeral = BookNumeral.ii;
               break;
            case BibleBook.iK:
               Numeral = BookNumeral.i;
               break;
            case BibleBook.iiK:
               Numeral = BookNumeral.ii;
               break;
            case BibleBook.iCh:
               Numeral = BookNumeral.i;
               break;
            case BibleBook.iiCh:
               Numeral = BookNumeral.ii;
               break;
            case BibleBook.iTh:
               Numeral = BookNumeral.i;
               break;
            case BibleBook.iiTh:
               Numeral = BookNumeral.ii;
               break;
            case BibleBook.iTm:
               Numeral = BookNumeral.i;
               break;
            case BibleBook.iiTm:
               Numeral = BookNumeral.ii;
               break;
            case BibleBook.iP:
               Numeral = BookNumeral.i;
               break;
            case BibleBook.iiP:
               Numeral = BookNumeral.ii;
               break;
            case BibleBook.iJn:
               Numeral = BookNumeral.i;
               break;
            case BibleBook.iiJn:
               Numeral = BookNumeral.ii;
               break;
            case BibleBook.iiiJn:
               Numeral = BookNumeral.iii;
               break;
            default:
               Numeral = BookNumeral.none;
               break;
         }
      }

      #endregion


      #region Public Methods

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
      /// Convert a book name string to the <c>BibleBook</c> enumeration value.
      /// </summary>
      /// <param name="bookName">This book name had better be valid, or an <c>ArgumentException</c> will be thrown.</param>
      /// <returns>A <c>BibleBook</c> enumeration value.</returns>
      public static BibleBook StringToBook(string bookName, BibleVersion version)
      {
         if (version.BookNames.Contains(bookName))
         {
            return (BibleBook)version.BookNames.IndexOf(bookName);
         }
         else
            throw new ArgumentException("The book name sent was not a valid book name in this version");
      }


      /// <summary>
      /// Gets the full chapter text formatted into paragraphs with verse numbers.
      /// </summary>
      /// <returns>A formatted paragrpah with verse numbers.</returns>
      public Paragraph GetChapterTextFormatted()
      {
         List<string> verses = Version.GetChapterVerses(this);

         Paragraph paragraph = new Paragraph();
         for (int i = 0; i < verses.Count; i++)
         {
            if (i > 0) // Don't number verse 1
            {
               Run number = new Run();
               number.Foreground = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColor"]);
               number.FontSize = 12;
               number.CharacterSpacing = 20;
               number.Text = " " + (i + 1).ToString() + Chars.NBSPACE;
               paragraph.Inlines.Add(number);
            }

            Run verse = new Run();
            verse.Text = verses[i];
            paragraph.Inlines.Add(verse);
         }

         return paragraph;
      }


      /// <summary>
      /// Get text for the audio reader.
      /// </summary>
      /// <returns>Plain text that is readable out loud. Each verse has a line return.</returns>
      public string GetChapterPlainText()
      {
         StringBuilder builder = new StringBuilder();
         List<string> verses = Version.GetChapterVerses(this);

         foreach(string verse in verses)
         {
            builder.AppendLine(verse);
         }

         return builder.ToString();
      }


      /// <summary>
      /// Cast a string to a Bible reference.
      /// </summary>
      /// <param name="reference">The original string</param>
      /// <returns>A reference if the string can be parsed, or null if not</returns>
      //public static BibleReference ToReference(string reference)
      //{
      //   string[] splitUpReference;

      //   int bookPrefix = 0;
      //   string bookName = null;
      //   ChapterVerse startChapter = null;
      //   ChapterVerse endChapter = null;

      //   splitUpReference = SplitUpReference(reference);

      //   // Got through each word or number group in a Bible reference
      //   for(int i = 0; i < splitUpReference.Length; i++)
      //   {
      //      // When the first word is a number, it denotes one of multiple books (eg. 1 John)
      //      // The array element after this will be the book name
      //      try
      //      {
      //         bookPrefix = int.Parse(splitUpReference[0]);
      //      }
      //      catch
      //      {
      //         bookPrefix = 0;
      //      }

      //      // Get the first element that can be a book name
      //      if(bookName == null && !ChapterVerse.IsChapterVerse(splitUpReference[i]))
      //      {
      //         bookName = splitUpReference[i];
      //      }

      //      // Get the first element that can be a chapter and verse
      //      else if(startChapter == null)
      //      {
      //         startChapter = new ChapterVerse(splitUpReference[i]);
      //      }
      //      else if(endChapter == null)
      //      {
      //         endChapter = new ChapterVerse(splitUpReference[i]);

      //         // Stop the loop at this point
      //         break;
      //      }
      //   }

      //   // Return a reference if it has been successfully parsed out
      //   if(bookName != null)
      //   {
      //      return new BibleReference();
      //   }
      //   else
      //   {
      //      return null;
      //   }
      //}

      /// <summary>
      /// Separate words and numbers that are stuck together
      /// </summary>
      /// <param name="lookupString">The user's reference search string</param>
      /// <returns>An array of strings representing words and number groups</returns>
      //public static string[] SplitUpReference(string lookupString)
      //{
      //   lookupString = SpaceOutNumbers(lookupString, "*");
      //   return lookupString.Split(WordSeparators, MAX_REFERENCE_WORDS, StringSplitOptions.RemoveEmptyEntries);
      //}
      #endregion


      #region Private Methods

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
      /// This helps know whether a reference contains many chapters and verses.
      /// Eg. 2 Chronicles 2:19-3:15 would contain 5 integers.
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


      /// <summary>
      /// Convert a numeral to a string.
      /// </summary>
      /// <param name="numeral">Book numeral</param>
      /// <param name="includeTrailingSpace">Whether to leave a space after the numeral so it concatenates properly</param>
      /// <returns>A string such as <c>II</c></returns>
      private static string NumeralToString(BookNumeral numeral, bool includeTrailingSpace = true)
      {
         switch(numeral)
         {
            case BookNumeral.i:
               return includeTrailingSpace ? "I " : "I";
            case BookNumeral.ii:
               return includeTrailingSpace ? "II " : "II";
            case BookNumeral.iii:
               return includeTrailingSpace ? "III " : "III";
            default:
               return string.Empty;
         }
      }

      #endregion


      #region Overrides

      /// <summary>
      /// Overridden string method.
      /// </summary>
      /// <returns>A typographically correct biblical reference.</returns>
      public override string ToString()
      {
         return BookName + " " + Chapter;
      }

      /// <summary>
      /// Implicit string operator (used by tabs).
      /// </summary>
      /// <param name="reference">The reference to convert to a string.</param>
      public static implicit operator string(BibleReference reference)
      {
         if (reference == null)
            return "New tab";
         else
            return reference.ToString();
      }

      #endregion
   }
}