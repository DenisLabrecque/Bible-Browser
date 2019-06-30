using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BibleBrowser
{
   /// <summary>
   /// Converts simple text to a better typographical standard by smartquoting and adding em-dashes.
   /// </summary>
   static class Text
   {
      private static char[] m_quoteCharacters = new char[] { '`', '\'', '"' };
      private static Dictionary<char, char> m_leftQuotes = new Dictionary<char, char>() { { '`', '‘' }, { '\'', '‘' }, { '"', '“' } };
      private static Dictionary<char, char> m_rightQuotes = new Dictionary<char, char>() { { '`', '’' }, { '\'', '’' }, { '"', '”' } };
      private const string MDASH = "—";

      /// <summary>
      /// Extension method that replaces accented characters with their unaccented equivalents.
      /// How do I remove diacritics (accents) from a string in .NET?
      /// Blair Conrad's answer
      /// </summary>
      /// <param name="text"></param>
      /// <returns></returns>
      public static string RemoveDiacritics(this string text)
      {
         var normalizedString = text.Normalize(NormalizationForm.FormD);
         var stringBuilder = new StringBuilder();

         foreach (var c in normalizedString)
         {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
               stringBuilder.Append(c);
            }
         }

         return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
      }

      /// <summary>
      /// Remove double spacing, standardize m-dashes, and use smart quotes.
      /// Formats simplified text to a literary and graphic design standard.
      /// </summary>
      public static string FormatTypographically(this string text)
      {
         StringBuilder builder = new StringBuilder();
         for(int i = 0; i < text.Length; i++)
         {
            // Words, numbers, punctuation, dashes, quotes, and apostrophes
            if (char.IsLetterOrDigit(text[i]) || char.IsPunctuation(text[i]) || m_quoteCharacters.Contains(text[i]))
            {
               // Mdashes
               if (text[i] == '-')
               {
                  if (i > 0 && text[i - 1] == '-')
                  {
                     continue;
                  }
                  // Hyphen
                  else if(i - 1 > 0 && i + 1 < text.Length && (char.IsLetterOrDigit(text[i - 1]) || char.IsLetterOrDigit(text[i + 1])))
                  {
                     builder.Append(text[i]);
                     continue;
                  }
                  // --
                  else if (i + 1 < text.Length && text[i] == '-' && (text[i + 1] == '-' || char.IsWhiteSpace(text[i + 1])))
                  {
                     builder.Append('—');
                     continue;
                  }
                  // End of line -
                  else if (i + 1 == text.Length)
                  {
                     builder.Append('—');
                     continue;
                  }
               }

               // Quotes and apostrophes
               else if (m_quoteCharacters.Contains(text[i]))
               {
                  // Quote character between text (apostrophe)
                  if (i > 0 && i < text.Length - 1 && m_quoteCharacters.Contains(text[i]) && char.IsLetterOrDigit(text[i - 1]) && char.IsLetterOrDigit(text[i + 1]))
                  {
                     builder.Append(m_rightQuotes['\'']); // Apostrophe
                     continue;
                  }
                  // Quote character before text (left quote)
                  else if (i < text.Length - 1 && m_quoteCharacters.Contains(text[i]) && char.IsLetterOrDigit(text[i + 1]))
                  {
                     builder.Append(m_leftQuotes[text[i]]);
                     continue;
                  }
                  // Quote character after text (right quote)
                  else if (i > 1 && m_quoteCharacters.Contains(text[i]) && (char.IsLetterOrDigit(text[i - 1]) || char.IsPunctuation(text[i - 1])))
                  {
                     builder.Append(m_rightQuotes[text[i]]);
                     continue;
                  }
               }

               // Letters, numbers, general punctuation
               else
               {
                  builder.Append(text[i]);
                  continue;
               }
            }

            // Whitespace (only between two characters)
            else if (char.IsWhiteSpace(text[i]))
            {
               // The last character is whitespace
               if (i + 1 == text.Length)
               {
                  continue;
               }
               // If the next character is whitespace, no need to include this one
               else if (i + 1 < text.Length && char.IsWhiteSpace(text[i + 1]))
               {
                  continue;
               }
               // Before an mdash
               else if (i + 1 < text.Length && text[i + 1] == '-')
               {
                  continue;
               }
               // After an mdash
               else if (i - 1 >= 0 && text[i - 1] == '-')
               {
                  continue;
               }
               else
               {
                  builder.Append(text[i]);
                  continue;
               }
            }
         }

         return builder.ToString();
      }
   }
}
