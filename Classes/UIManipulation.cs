using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace BibleBrowserUWP
{
   /// <summary>
   /// Generic manipulations to the UI.
   /// </summary>
   public static class UIManipulation
   {
      /// <summary>
      /// Append any string as a run into a <c>RichTextBlock</c>.
      /// </summary>
      /// <param name="textBlock">The text block that the string is to be part of.</param>
      /// <param name="text">The text to add to the text block.</param>
      public static void AddTextToRichTextBlock(RichTextBlock textBlock, string text)
      {
         Paragraph paragraph = new Paragraph();
         Run run = new Run();
         run.Text = text;
         paragraph.Inlines.Add(run);
         textBlock.Blocks.Add(paragraph);
      }

      internal static void AddTextToStackPanel(StackPanel stackPanel, string text)
      {
         TextBlock paragraph = new TextBlock();
         paragraph.Text = text;
         paragraph.IsTextSelectionEnabled = true;
         paragraph.TextWrapping = Windows.UI.Xaml.TextWrapping.WrapWholeWords;
         paragraph.FontFamily = new Windows.UI.Xaml.Media.FontFamily("Verdana");
         stackPanel.Children.Add(paragraph);
      }


      /// <summary>
      /// Remove all block elements from a <c>RichTextBlock</c>.
      /// </summary>
      public static void RemoveAllBlocks(RichTextBlock textBlock)
      {
         while (textBlock.Blocks.Count > 0)
            textBlock.Blocks.RemoveAt(0);
      }
   }
}
