using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BibleBrowser
{
   /// <summary>
   /// Class used to compare and print the chapter and verse element of a Bible reference, such as 2:31, or 3:32-34b.
   /// </summary>
   class ChapterVerse
   {
      #region Properties
      public int Chapter { get; private set; }
      public int Verse { get; private set; }
      public int EndVerse { get; private set; }
      public string Subscript { get; private set; }
      #endregion

      #region Fields
      public static char[] ChapterSeparators = {
         ':',
         '.'
      };
      public static char[] VerseSeparators = {
         '-',
         '+'
      };
      public static char[] VerseSubscripts = {
         'a',
         'b',
         'c'
      };
      #endregion

      #region Constructors
      /// <summary>
      /// Default constructor.
      /// </summary>
      public ChapterVerse()
      {
         InitializeValues();
      }

      /// <summary>
      /// Constructor with only a chapter.
      /// </summary>
      public ChapterVerse(int chapter)
      {
         InitializeValues();

         // Check and set values
         if(chapter < 0)
         {
            throw new Exception("A chapter is no less than 1, or zero if undetermined");
         }
         else
         {
            Chapter = chapter;
         }
      }

      /// <summary>
      /// Constructor with only a chapter and a verse.
      /// </summary>
      public ChapterVerse(int chapter, int verse)
      {
         InitializeValues();

         // Check and set values
         if(chapter < 0)
         {
            throw new Exception("A chapter is no less than 1, or zero if undetermined");
         }
         else
         {
            Chapter = chapter;
         }

         if(verse < 0)
         {
            throw new Exception("A verse is no less than 1, or zero if undetermined");
         }
         else
         {
            Verse = verse;
         }
      }

      /// <summary>
      /// Constructor with only a chapter, a verse, and a subscript.
      /// </summary>
      /// <param name="subscript">A single-character subscript such as a, b, or c, to denote only a certain fraction of a verse.</param>
      public ChapterVerse(int chapter, int verse, string subscript)
      {
         InitializeValues();

         // Check and set values
         if(chapter < 0)
         {
            throw new Exception("A chapter is no less than 1, or zero if undetermined");
         }
         else
         {
            Chapter = chapter;
         }

         if(chapter == 0 && verse != 0)
         {
            throw new Exception("A verse cannot be set without a determinate chapter");
         }
         else if(verse < 0)
         {
            throw new Exception("A verse is no less than 1, or zero if undetermined");
         }
         else
         {
            Verse = verse;
         }

         if(verse == 0 && subscript != string.Empty)
         {
            throw new Exception("A verse subscript cannot be set without a determinate verse");
         }
         else if(subscript == "a" || subscript == "b" || subscript == "c" || subscript == "")
         {
            Subscript = subscript;
         }
         else
         {
            throw new Exception("A verse subscript is either a, b, c, or empty if undetermined");
         }
      }

      /// <summary>
      /// Constructor with a string to cast
      /// </summary>
      /// <param name="chapterWithVerse">String to cast to a chapter and verse. This string must have one and only one chapter and verse.</param>
      public ChapterVerse(string chapterWithVerse)
      {
         // Check whether there is truly one and only one chapter and verse
         string[] chapterColonVerse = chapterWithVerse.Split(ChapterVerse.ChapterSeparators, StringSplitOptions.RemoveEmptyEntries);
         if(chapterColonVerse.Length > 2)
         {
            throw new Exception("There was more than one chapter and verse in the cast");
         }
         else
         {
            int chapterNumber = 0;
            int verseNumber = 0;
            int endVerseNumber = 0;

            // If the verse number can't be read, just leave it at 0
            int.TryParse(chapterColonVerse[0], out chapterNumber);

            // Go through all possible verse numbers
            if(chapterColonVerse.Length == 2)
            {
               // Find how many verse numbers there are
               string[] verseNumberList = chapterColonVerse[1].Split(ChapterVerse.VerseSeparators, StringSplitOptions.RemoveEmptyEntries);

               // Assign beginning and end verses
               switch(verseNumberList.Length)
               {
                  // There is only a beginning verse
                  case 1:
                     int.TryParse(verseNumberList[0], out verseNumber);
                     break;
                  // There is a beginning and end verse
                  case 2:
                     int.TryParse(verseNumberList[0], out verseNumber);
                     int.TryParse(verseNumberList[1], out endVerseNumber);
                     break;
                  // There are many beginning and end verses
                  case 3:
                     int.TryParse(verseNumberList[0], out verseNumber);
                     int.TryParse(verseNumberList[verseNumberList.Length - 1], out endVerseNumber);
                     break;
               }
            }

            // Set the values
            Chapter = chapterNumber;
            Verse = verseNumber;
            EndVerse = endVerseNumber;
         }
      }
      #endregion

      #region Private Methods
      /// <summary>
      /// Compare a character to the list of acceptable chapter and verse characters.
      /// </summary>
      /// <param name="character">The character to compare</param>
      /// <returns>True if the character is acceptable, or false if not</returns>
      private static bool IsAcceptableCharacter(char character)
      {
         if(char.IsDigit(character))
         {
            return true;
         }
         else
         {
            foreach(char symbol in ChapterSeparators)
            {
               if(character == symbol)
               {
                  return true;
               }
            }

            foreach(char symbol in VerseSeparators)
            {
               if(character == symbol)
               {
                  return true;
               }
            }

            foreach(char letter in VerseSubscripts)
            {
               if(character == letter)
               {
                  return true;
               }
            }

            return false;
         }
      }

      /// <summary>
      /// Set all values to zero or empty. To be used in the constructors only.
      /// </summary>
      void InitializeValues()
      {
         Chapter = 0;
         Verse = 0;
         EndVerse = 0;
         Subscript = string.Empty;
      }
      #endregion

      #region Public Methods
      /// <summary>
      /// Check some text to see whether it is a verse and chapter, or a range of verses and chapters.
      /// </summary>
      /// <param name="text"></param>
      /// <returns></returns>
      public static bool IsChapterVerse(string text)
      {
         foreach(char character in text)
         {
            if(!IsAcceptableCharacter(character))
            {
               return false;
            }
         }

         return true;
      }

      /// <summary>
      /// Overridden string method that prints a chapter and verse class.
      /// </summary>
      /// <returns>A string in the form #:#s</returns>
      public override string ToString()
      {
         if(Chapter > 0 && Verse > 0)
         {
            return Chapter + ":" + Verse + Subscript;
         }
         else if(Chapter > 0)
         {
            return Chapter.ToString();
         }
         else
         {
            return string.Empty;
         }
      }

      /// <summary>
      /// Overridden hash code method
      /// </summary>
      /// <returns>The addition of chapter and verse</returns>
      public override int GetHashCode()
      {
         return Chapter + Verse;
      }

      /// <summary>
      /// Equality operation override
      /// </summary>
      /// <param name="obj">The object to be compared to this class</param>
      /// <returns>True if the compared class equals this class; false if not</returns>
      public override bool Equals(object obj)
      {
         if(obj == null || !this.GetType().Equals(obj.GetType()))
         {
            return false;
         }

         // See Microsoft Visual C# 8th ed. page 506 for implementation
         if(obj is ChapterVerse)
         {
            ChapterVerse compare = (ChapterVerse)obj;
            return (this.Chapter == compare.Chapter) && (this.Verse == compare.Verse);
         }
         else
         {
            return false;
         }
      }
      #endregion

      #region Overloaded Operators
      /// <summary>
      /// Greater than operator overload.
      /// </summary>
      public static bool operator >(ChapterVerse a, ChapterVerse b)
      {
         if(a.Chapter > b.Chapter)
         {
            return true;
         }
         else if(a.Verse > b.Verse)
         {
            return true;
         }
         else
         {
            return false;
         }
      }

      /// <summary>
      /// Whether a chapter is greater than a number.
      /// </summary>
      /// <param name="a"></param>
      /// <param name="i"></param>
      /// <returns></returns>
      public static bool operator >(ChapterVerse a, int i)
      {
         if(a.Chapter > i)
         {
            return true;
         }
         else
         {
            return false;
         }
      }

      /// <summary>
      /// Whether a chapter is less than a number.
      /// </summary>
      /// <param name="a"></param>
      /// <param name="i"></param>
      /// <returns></returns>
      public static bool operator <(ChapterVerse a, int i)
      {
         if(a.Chapter < i)
         {
            return true;
         }
         else
         {
            return false;
         }
      }

      /// <summary>
      /// Less than operator overload.
      /// </summary>
      public static bool operator <(ChapterVerse a, ChapterVerse b)
      {
         return !(a > b);
      }

      /// <summary>
      /// Equality operator overload.
      /// </summary>
      public static bool operator ==(ChapterVerse a, ChapterVerse b)
      {
         if(ReferenceEquals(a, null))
         {
            if(ReferenceEquals(b, null))
            {
               return true;
            }
            else
            {
               return false;
            }
         }
         else
         {
            return a.Equals(b);
         }
      }

      /// <summary>
      /// Inequality operator overload.
      /// </summary>
      public static bool operator !=(ChapterVerse a, ChapterVerse b)
      {
         if(a == null)
            return false;
         else if(b == null)
            return false;

         return !a.Equals(b);
      }
      #endregion
   }
}
