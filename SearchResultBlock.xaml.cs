using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Documents;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace BibleBrowserUWP
{
   public sealed partial class SearchResultBlock : UserControl
   {
      public SearchResultBlock()
      {
         this.InitializeComponent();
      }

      public SearchResult Result {
         get { return (SearchResult)GetValue(ResultProperty); }
         set { SetValue(ResultProperty, value); }
      }

      public static readonly DependencyProperty ResultProperty =
          DependencyProperty.Register("Result", typeof(SearchResult), typeof(SearchResultBlock), new PropertyMetadata(null, new PropertyChangedCallback(Result_Changed)));

      private static void Result_Changed(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
      {
         if (e.NewValue != null && e.NewValue is SearchResult data)
         {
            SearchResultBlock result = dependencyObject as SearchResultBlock;

            // Add the reference
            result.Reference.Text = data.FullReference;

            // Create runs for the normal text and the highlighted text
            result.ResultBlock.Inlines.Clear();
            string[] resultText = data.DisplayText.Split(data.HighlightText);
            result.ResultBlock.Inlines.Add(new Run { Text = resultText.First() });
            result.ResultBlock.Inlines.Add(new Run { Text = data.HighlightText, Foreground = new SolidColorBrush(Color.FromArgb(255,226,63,71)) });
            if (resultText.Length > 1)
               result.ResultBlock.Inlines.Add(new Run { Text = resultText.Last() });
         }
      }
   }
}
