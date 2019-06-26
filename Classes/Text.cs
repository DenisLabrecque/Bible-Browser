using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BibleBrowser
{
   static class Text
   {
      private static char[] m_quoteCharacters = new char[] { '`', '\'', '"' };
      private static Dictionary<char, char> m_leftQuotes = new Dictionary<char, char>() { { '`', '‘' }, { '\'', '‘' }, { '"', '“' } };
      private static Dictionary<char, char> m_rightQuotes = new Dictionary<char, char>() { { '`', '’' }, { '\'', '’' }, { '"', '”' } };
      private const string MDASH = "—";

      /// <summary>
      /// Remove double spacing, standardize m-dashes, and use smart quotes.
      /// </summary>
      public static string FormatTypographically(this string text)
      {
         StringBuilder builder = new StringBuilder();
         for(int i = 0; i < text.Length; i++)
         {
            // Ignore words
            if (char.IsLetterOrDigit(text[i]) || char.IsPunctuation(text[i]))
            {
               builder.Append(text[i]);
               continue;
            }
            // Only include whitespace between two characters
            else if (char.IsWhiteSpace(text[i]))
            {
               // If the next character is whitespace, no need to include this one
               if (i + 1 < text.Length && char.IsWhiteSpace(text[i + 1]))
               {
                  continue;
               }
               // Previous m-dash
               else if(i - 2 > 0 && (text[i - 1] == '-' && text[i - 2] == '-') || (text[i - 1] == '-' && char.IsWhiteSpace(text[i - 2])))
               {
                  continue;
               }
               // An m-dash
               else if(i + 2 < text.Length && (text[i + 1] == '-' && text[i + 2] == '-') || (text[i + 1] == '-' && char.IsWhiteSpace(text[i + 2])))
               {
                  builder.Append('—');
                  i += 2;
                  continue;
               }
               else
               {
                  builder.Append(text[i]);
                  continue;
               }
            }
            // Quote character between text (apostrophe)
            else if (i > 0 && i < text.Length - 1 && m_quoteCharacters.Contains(text[i]) && char.IsLetterOrDigit(text[i - 1]) && char.IsLetterOrDigit(text[i + 1]))
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
            else if (i > 1 && m_quoteCharacters.Contains(text[i]) && char.IsLetterOrDigit(text[i - 1]))
            {
               builder.Append(m_rightQuotes[text[i]]);
               continue;
            }
         }

         return builder.ToString();
      }
   }
}
