using BibleBrowser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BibleBrowserUWP
{
   /// <summary>
   /// An empty page that can be used on its own or navigated to within a Frame.
   /// </summary>
   public sealed partial class MainPage : Page
   {
      #region Member Variables and Constants

      // The media object for controlling and playing audio.
      MediaElement m_mediaElement = new MediaElement();

      // The object for controlling the speech synthesis engine (voice).
      SpeechSynthesizer m_synth = new SpeechSynthesizer();
      bool m_isPlaybackStarted = false;

      const int MINMARGIN = 90;
      const int MAXTEXTWIDTH = 600;

      #endregion


      #region Properties

      TrulyObservableCollection<BrowserTab> Tabs { get => BrowserTab.Tabs; }

      #endregion

      public MainPage()
      {
         this.InitializeComponent();
         StyleTitleBar();

         // Open the tab that was active before the app was last closed
         lvTabs.SelectedItem = BrowserTab.Selected;

         // Ensure the text remains within the window size
         this.SizeChanged += MainPage_SizeChanged;
         // Load previous tabs when the app opens
         Application.Current.LeavingBackground += new LeavingBackgroundEventHandler(App_LeavingBackground);
         // Save tabs when the app closes
         Application.Current.Suspending += new SuspendingEventHandler(App_Suspending);
      }



      /// <summary>
      /// Fires when the window size is changed by dragging, snapping, or pixel density.
      /// </summary>
      private void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
      {
         // Reduce the main text to fit within margins
         if (e.NewSize.Width < (2 * MINMARGIN) + MAXTEXTWIDTH)
         {
            rtbVerses.Width = e.NewSize.Width - (2 * MINMARGIN);
         }
         // The margins must increase to center the text
         else
         {
            rtbVerses.Width = MAXTEXTWIDTH;
         }
      }


      /// <summary>
      /// Fires when the app is opened, and when the app gets re-selected.
      /// </summary>
      async void App_LeavingBackground(Object sender, LeavingBackgroundEventArgs e)
      {
         Debug.WriteLine("App leaving background!");
         await BrowserTab.LoadSavedTabs();
      }


      /// <summary>
      /// Fires whenever the user switches to another app, the desktop, or the Start screen
      /// Save the currently open tabs to an XML file.
      /// </summary>
      async void App_Suspending(Object sender, SuspendingEventArgs e)
      {
         await BrowserTab.SaveOpenTabs();
         Debug.WriteLine("The app is suspending!");
      }


      /// <summary>
      /// Get the main text and begin reading it asynchroniously.
      /// </summary>
      async private void ReadMainTextAloud()
      {
         if(m_isPlaybackStarted)
         {
            m_mediaElement.Play();
         }
         else
         {
            // Generate the audio stream from plain text.
            SpeechSynthesisStream stream = await m_synth.SynthesizeTextToStreamAsync(BrowserTab.Selected.Reference.GetChapterPlainText());

            // Send the stream to the media object.
            m_mediaElement.SetSource(stream, stream.ContentType);
            m_mediaElement.Play();
            m_isPlaybackStarted = true;
         }
      }


      /// <summary>
      /// Hide the default title bar to create a custom look instead.
      /// </summary>
      private void StyleTitleBar()
      {
         // Hide default title bar
         CoreApplicationViewTitleBar titleBar = CoreApplication.GetCurrentView().TitleBar;
         ApplicationViewTitleBar appBar = ApplicationView.GetForCurrentView().TitleBar;
         titleBar.ExtendViewIntoTitleBar = true;
         appBar.ButtonBackgroundColor = Colors.Transparent;
         //appBar.ButtonHoverBackgroundColor = Colors.Transparent;
         //appBar.ButtonPressedBackgroundColor = Colors.Transparent;
         appBar.ButtonInactiveBackgroundColor = Colors.Transparent;
         UpdateTitleBarLayout(titleBar);

         // Set XAML element as a draggable region.
         Window.Current.SetTitleBar(grdTitleBar);

         // Register a handler for when the size of the overlaid caption control changes.
         // For example, when the app moves to a screen with a different DPI.
         titleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;

         // Register a handler for when the title bar visibility changes.
         // For example, when the title bar is invoked in full screen mode.
         titleBar.IsVisibleChanged += CoreTitleBar_IsVisibleChanged;
      }


      /// <summary>
      /// Get the size of the caption controls area and back button 
      /// (returned in logical pixels), and move content around as necessary.
      /// </summary>
      /// <param name="coreTitleBar"></param>
      private void UpdateTitleBarLayout(CoreApplicationViewTitleBar coreTitleBar)
      {
         LeftPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayLeftInset);
         RightPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayRightInset);

         // Update title bar control size as needed to account for system size changes.
         grdTitleBar.Height = coreTitleBar.Height;
      }

      private void CoreTitleBar_IsVisibleChanged(CoreApplicationViewTitleBar sender, object args)
      {
         if(sender.IsVisible)
         {
               grdTitleBar.Visibility = Visibility.Visible;
         }
         else
         {
               grdTitleBar.Visibility = Visibility.Collapsed;
         }
      }


      #region Events

      private void BtnPlay_Click(object sender, RoutedEventArgs e)
      {
         ReadMainTextAloud();
         btnPlay.Visibility = Visibility.Collapsed;
         btnPause.Visibility = Visibility.Visible;
      }

      private void BtnPause_Click(object sender, RoutedEventArgs e)
      {
         m_mediaElement.Pause();
         btnPause.Visibility = Visibility.Collapsed;
         btnPlay.Visibility = Visibility.Visible;
      }


      /// <summary>
      /// Go to the previous reference in the current tab's history.
      /// </summary>
      private void BtnPrevious_Click(object sender, RoutedEventArgs e)
      {
         BibleReference reference = BrowserTab.Selected.Reference;
         BrowserTab.Selected.GoToReference(ref reference, BrowserTab.NavigationMode.Previous);
         PrintChapter(reference);
         ActivateButtons();
      }


      /// <summary>
      /// Go to the next reference in the current tab's history.
      /// </summary>
      private void BtnNext_Click(object sender, RoutedEventArgs e)
      {
         BibleReference reference = BrowserTab.Selected.Reference;
         BrowserTab.Selected.GoToReference(ref reference, BrowserTab.NavigationMode.Next);
         PrintChapter(reference);
         ActivateButtons();
      }

      private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
      {
         UpdateTitleBarLayout(sender);
      }


      /// <summary>
      /// A new tab was selected; track this in app memory.
      /// Load the new page contents according to the reference of the newly selected tab.
      /// </summary>
      private void LvTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         // Some event caused no tab to be selected; we always want a tab selected, so override that
         if(lvTabs.SelectedIndex == -1)
         {
            lvTabs.SelectedIndex = BrowserTab.SelectedIndex; // HACK prevent -1 selection
            return;
         }

         BrowserTab.SelectedIndex = lvTabs.SelectedIndex;
         BrowserTab selected = (BrowserTab)lvTabs.SelectedItem;
         if (selected == null)
         {
            // Keep the previous selection
            throw new Exception("No selected tab found");
         }

         // Stop audio playback when changing tabs
         m_isPlaybackStarted = false;
         m_mediaElement.Stop();
         btnPause.Visibility = Visibility.Collapsed;
         btnPlay.Visibility = Visibility.Visible;

         // Only display the reference when there is still a tab to show; if not, we are closing the app anyway
         if (lvTabs.Items.Count > 0)
         {
            PrintChapter(BrowserTab.Selected.Reference);
         }

         // Focus on the search bar
         asbSearch.Focus(FocusState.Programmatic);

         ActivateButtons();
      }
      

      /// <summary>
      /// Based on the current tab, decide whether the Previous, Next, and Play commands should be clickable.
      /// </summary>
      private void ActivateButtons()
      {
         // New tab: no button should be clickable
         if (BrowserTab.Selected.Reference == null)
         {
            btnPrevious.IsEnabled = false;
            btnNext.IsEnabled = false;
            btnPlay.IsEnabled = false;
         }
         // There is text already displayed: check whether there is history
         else
         {
            btnPlay.IsEnabled = true;

            // There is a history of references
            if (BrowserTab.Selected.History.Count >= 2)
            {
               if (BrowserTab.Selected.Next == null)
                  btnNext.IsEnabled = false;
               else
                  btnNext.IsEnabled = true;

               if (BrowserTab.Selected.Previous == null)
                  btnPrevious.IsEnabled = false;
               else
                  btnPrevious.IsEnabled = true;
            }
            // There is no history
            else
            {
               btnPrevious.IsEnabled = false;
               btnNext.IsEnabled = false;
            }
         }
      }


      /// <summary>
      /// Print a chapter of the Bible to the app page according to the reference sent.
      /// </summary>
      /// <param name="reference">The chapter to print.</param>
      private void PrintChapter(BibleReference reference)
      {
         // Remove previous contents
         rtbVerses.Blocks.Clear();

         // New tab, leave blank
         if (BrowserTab.Selected.Reference == null)
            return;
         else
            rtbVerses.Blocks.Add(reference.GetChapterTextFormatted());
      }


      /// <summary>
      /// Close a tab.
      /// </summary>
      private void BtnCloseTab_Click(object sender, RoutedEventArgs e)
      {
         // There is still another tab to show when one is removed
         if (lvTabs.Items.Count >= 2)
         {
            Guid tabGuid = ((Guid)((Button)sender).Tag);
            BrowserTab removeTab = Tabs.Single(p => p.Guid == tabGuid);
            int removeIndex = Tabs.IndexOf(removeTab);

            // The selected tab is being removed
            if (removeIndex == lvTabs.SelectedIndex)
            {
               if (removeIndex == Tabs.Count - 1)
                  lvTabs.SelectedIndex = Tabs.Count - 2;
               else if (removeIndex == 0)
                  lvTabs.SelectedIndex = 1;
               else
                  lvTabs.SelectedIndex = removeIndex + 1;
            }

            Tabs.RemoveAt(removeIndex);
         }
         // There is no new tab to show; close the app
         else
         {
            CoreApplication.Exit();
         }

         asbSearch.Focus(FocusState.Keyboard);
         ActivateButtons();
      }


      /// <summary>
      /// Open a new tab.
      /// </summary>
      private void BtnNewTab_Click(object sender, RoutedEventArgs e)
      {
         Tabs.Add(new BrowserTab());
         lvTabs.SelectedIndex = Tabs.Count - 1;
         asbSearch.Text = string.Empty;
         ActivateButtons();
      }
      #endregion

      private void AsbSearch_GotFocus(object sender, RoutedEventArgs e)
      {
         HideAllDropdowns();
      }

      private void AsbSearch_LostFocus(object sender, RoutedEventArgs e)
      {
         ShowAllDropdowns();
      }

      ///// <summary>
      ///// Set version, book, and chapter selection box visibility and content.
      ///// </summary>
      //private void DropdownVisibilityAndText()
      //{
      //   BibleReference reference = BrowserTab.Selected.Reference;
      //   if(reference == null)
      //   {
      //      HideAllDropdowns();
      //   }
      //   else
      //   {

      //      // Decide whether to show or hide dropdown buttons
      //      if(asbSearch.Text == null && (asbSearch.FocusState == FocusState.Pointer || asbSearch.FocusState == FocusState.Keyboard))
      //      {
      //         HideAllDropdowns();
      //      }
      //      else
      //      {
      //         ShowAllDropdowns();
      //      }
      //   }
      //}


      /// <summary>
      /// Set version, book, and chapter selection boxes to be hidden.
      /// </summary>
      private void HideAllDropdowns()
      {
         BibleReference reference = BrowserTab.Selected.Reference;
         if(reference == null)
         {
            asbSearch.PlaceholderText = "Search or enter reference";
         }
         else
         {
            asbSearch.Text = reference.Version + ": " + reference.ToString();
            asbSearch.SelectAll();
         }

         ddbVersion.Visibility = Visibility.Collapsed;
         ddbBook.Visibility = Visibility.Collapsed;
         ddbChapter.Visibility = Visibility.Collapsed;
      }


      /// <summary>
      /// Set version, book, and chapter selection boxes to be visible.
      /// </summary>
      private void ShowAllDropdowns()
      {
         BibleReference reference = BrowserTab.Selected.Reference;

         // Fill dropdowns with content
         gvVersions.ItemsSource = BibleLoader.Bibles;
         gvBooks.ItemsSource = reference.Version.BookNames;
         gvChapters.ItemsSource = reference.Chapters;

         asbSearch.Text = string.Empty;
         asbSearch.PlaceholderText = string.Empty;
         
         ddbVersion.Content = reference.Version;
         ddbBook.Content = reference.BookName;
         ddbChapter.Content = reference.Chapter;

         ddbVersion.Visibility = Visibility.Visible;
         ddbBook.Visibility = Visibility.Visible;
         ddbChapter.Visibility = Visibility.Visible;
      }

      private async void Home_Click(object sender, RoutedEventArgs e)
      {
         await BrowserTab.SaveOpenTabs();
      }

      private void AsbSearch_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
      {
         if(e.Key == Windows.System.VirtualKey.Enter)
         {
            // Have a reference to go to ready
            BibleReference reference = BrowserTab.Selected.Reference;
            if (reference == null)
               reference = new BibleReference(BibleLoader.DefaultVersion, BibleBook.Gn);

            // Separate the query into book name and verse
            List<string> queryElements = ((TextBox)sender).Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            int bookNumeral = 0;
            string bookName = null;
            int chapter = 0;

            // There must only be a book name
            if (queryElements.Count == 1)
            {
               bookName = BibleSearch.ClosestBookName(reference.Version, queryElements[0]);
            }
            else
            {
               int[] numberMatrix = new int[queryElements.Count]; // Where numbers are in the query elements

               // Determine where the numbers are in the query elements
               int number = 0;
               for (int i = 0; i < queryElements.Count; i++)
               {
                  int.TryParse(queryElements[i], out number); // A fault here is that Roman numerals won't work, and the book name is assigned to the numeral TODO
                  if (number != 0)
                     numberMatrix[i] = Math.Abs(number);
                  else
                     numberMatrix[i] = 0;
               }

               // Assign book number, book name, and chapter in sequence
               for (int i = 0; i < queryElements.Count; i++)
               {
                  // Book numeral
                  if (bookName == null && bookNumeral == 0 && numberMatrix[i] != 0)
                  {
                     bookNumeral = numberMatrix[i];
                     Debug.WriteLine("Book numeral entered as " + bookNumeral);
                     continue;
                  }
                  // Book Name
                  else if (bookName == null && numberMatrix[i] == 0)
                  {
                     bookName = queryElements[i];
                     continue;
                  }
                  // Chapter
                  else if (chapter == 0 && numberMatrix[i] != 0)
                  {
                     chapter = numberMatrix[i];
                     break; // Stop here
                  }
               }

               // Add the book numeral to the book name
               if (bookNumeral != 0)
               {
                  bookName = bookNumeral + " " + bookName;
               }
               Debug.WriteLine("The user entered " + bookName + " " + chapter);
            }

            // Display the reference and contents
            if (bookName == null)
            {
               if (chapter == 0)
               {
                  // Don't change anything
               }
               else
               {
                  reference = new BibleReference(reference.Version, reference.Book, chapter);
                  BrowserTab.Selected.GoToReference(ref reference, BrowserTab.NavigationMode.Add);
               }
            }
            else
            {
               // Convert the book to the closest valid book
               bookName = BibleSearch.ClosestBookName(reference.Version, bookName);
               if (chapter == 0)
               {
                  reference = new BibleReference(reference.Version, BibleReference.StringToBook(bookName, reference.Version));
                  BrowserTab.Selected.GoToReference(ref reference, BrowserTab.NavigationMode.Add);
               }
               else
               {
                  reference = new BibleReference(reference.Version, BibleReference.StringToBook(bookName, reference.Version), chapter);
                  BrowserTab.Selected.GoToReference(ref reference, BrowserTab.NavigationMode.Add);
               }
            }
            ShowAllDropdowns();
            PrintChapter(reference);            
         }
      }

      private void MfiAddBible_Click(object sender, RoutedEventArgs e)
      {
         PickNewBibleAsync();
      }

      private async void PickNewBibleAsync()
      {
         var picker = new Windows.Storage.Pickers.FileOpenPicker();
         picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
         picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
         picker.FileTypeFilter.Add(".xml");

         StorageFile file = await picker.PickSingleFileAsync();
         if (file != null)
         {
            // Save the file to the app directory

         }
         else
         {
            // Cancelled
         }
      }


      /// <summary>
      /// Go to the version the user clicks and show the books flyout.
      /// </summary>
      private void GvVersions_ItemClick(object sender, ItemClickEventArgs e)
      {
         // Get the version the user clicked
         BibleVersion version = (BibleVersion)e.ClickedItem;

         // Go to the version in the present reference
         BibleReference oldReference = BrowserTab.Selected.Reference;
         BibleReference newReference = new BibleReference(version, oldReference.Book, oldReference.Chapter);
         BrowserTab.Selected.GoToReference(ref newReference, BrowserTab.NavigationMode.Add);

         flyVersion.Hide();
         flyBook.ShowAt(ddbBook);
      }


      /// <summary>
      /// Go to the book the user clicks and show the chapters flyout.
      /// </summary>
      private void GvBooks_ItemClick(object sender, ItemClickEventArgs e)
      {
         // Get the name of the book the user clicked
         string book = (string)e.ClickedItem;

         // Go to the book in the present reference
         BibleVersion version = BrowserTab.Selected.Reference.Version;
         BibleReference reference = new BibleReference(version, BibleReference.StringToBook(book, version));
         BrowserTab.Selected.GoToReference(ref reference, BrowserTab.NavigationMode.Add);
         
         flyBook.Hide();
         flyChapter.ShowAt(ddbChapter);
      }


      /// <summary>
      /// Go to the chapter the user clicks and hide the flyout.
      /// </summary>
      private void GvChapters_ItemClick(object sender, ItemClickEventArgs e)
      {
         // Get the chapter number the user clicked
         int chapter = (int)e.ClickedItem;

         // Go to the book in the present reference
         BibleReference oldReference = BrowserTab.Selected.Reference;
         BibleReference newReference = new BibleReference(oldReference.Version, oldReference.Book, chapter);
         BrowserTab.Selected.GoToReference(ref newReference, BrowserTab.NavigationMode.Add);

         flyChapter.Hide();
      }
   }
}