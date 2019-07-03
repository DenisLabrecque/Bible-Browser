using BibleBrowser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BibleBrowserUWP
{
   /// <summary>
   /// An empty page that can be used on its own or navigated to within a Frame.
   /// </summary>
   public sealed partial class MainPage : Page
   {
      #region Constants

      const int MINMARGIN = 90;
      const int MAXTEXTWIDTH = 600;

      #endregion


      #region Member Variables

      // The media object for controlling and playing audio.
      MediaElement m_mediaElement = new MediaElement();

      // The object for controlling the speech synthesis engine (voice).
      SpeechSynthesizer m_synth = new SpeechSynthesizer();

      bool m_isPlaybackStarted = false;
      bool m_areTabsLoaded = false;

      #endregion


      #region Properties

      TrulyObservableCollection<BrowserTab> Tabs { get => BrowserTab.Tabs; }
      List<BibleVersion> Bibles { get => BibleLoader.Bibles; } // TODO change to observable collection?

      #endregion


      #region Page Initialization and Events

      public MainPage()
      {
         this.InitializeComponent();
         EraseText();
         HideAllDropdowns(); // Don't show Genesis 1
         
         // Load previous tabs when the app opens
         Application.Current.LeavingBackground += new LeavingBackgroundEventHandler(App_LeavingBackground);

         StyleTitleBar();
         cbDefaultVersion.SelectedItem = BibleVersion.DefaultVersion;
         // Ensure the text remains within the window size
         this.SizeChanged += MainPage_SizeChanged;

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

         // Set the maximum width of the tab area
         CoreApplicationViewTitleBar titleBar = CoreApplication.GetCurrentView().TitleBar;
         //((StackPanel)lvTabs.ItemsPanelRoot).MaxWidth = e.NewSize.Width - (titleBar.SystemOverlayLeftInset + titleBar.SystemOverlayRightInset);
         spTabArea.MaxWidth = e.NewSize.Width - (titleBar.SystemOverlayLeftInset + titleBar.SystemOverlayRightInset);
      }

      /// <summary>
      /// Fires when the app is opened, and when the app gets re-selected.
      /// </summary>
      async void App_LeavingBackground(Object sender, LeavingBackgroundEventArgs e)
      {
         Debug.WriteLine("App leaving background!");
         if (m_areTabsLoaded == false)
         {
            await BrowserTab.LoadSavedTabs();
            m_areTabsLoaded = true;

            // Open the tab that was active before the app was last closed
            lvTabs.SelectedItem = BrowserTab.Selected;
         }
      }

      /// <summary>
      /// Fires whenever the user switches to another app, the desktop, or the Start screen
      /// Save the currently open tabs to an XML file.
      /// </summary>
      async void App_Suspending(Object sender, SuspendingEventArgs e)
      {
         SuspendingDeferral defer = e.SuspendingOperation.GetDeferral(); // Wait while we asynchronously create the xml document
         await BrowserTab.SaveOpenTabs();
         Debug.WriteLine("The app is suspending!");
         defer.Complete();
      }


      private void CoreTitleBar_IsVisibleChanged(CoreApplicationViewTitleBar sender, object args)
      {
         if (sender.IsVisible)
         {
            grdTitleBar.Visibility = Visibility.Visible;
         }
         else
         {
            grdTitleBar.Visibility = Visibility.Collapsed;
         }
      }

      private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
      {
         UpdateTitleBarLayout(sender);
      }

      #endregion


      #region Methods

      /// <summary>
      /// Go to the previous reference in the current tab's history.
      /// </summary>
      private void PreviousReference()
      {
         if (BrowserTab.Selected.Previous != null)
         {
            Debug.WriteLine("Previous reference called!");
            BibleReference reference = BrowserTab.Selected.Reference;
            BrowserTab.Selected.GoToReference(ref reference, BrowserTab.NavigationMode.Previous);
            PrintChapter(reference);
            ActivateButtons();
         }
      }

      /// <summary>
      /// Go to the next reference in the current tab's history.
      /// </summary>
      private void NextReference()
      {
         if (BrowserTab.Selected.Next != null)
         {
            Debug.WriteLine("Next reference called!");
            BibleReference reference = BrowserTab.Selected.Reference;
            BrowserTab.Selected.GoToReference(ref reference, BrowserTab.NavigationMode.Next);
            PrintChapter(reference);
            ActivateButtons();
         }
      }

      /// <summary>
      /// Display the chapter previous to the current one.
      /// </summary>
      private void PreviousChapter()
      {
         BibleReference oldReference = BrowserTab.Selected.Reference;

         if(oldReference != null) // Not a new tab
         {
            int chapter = oldReference.Chapter;
            BibleBook book = oldReference.Book;

            chapter--;
            if (chapter < 1)
            {
               int bookIndex = (int)book - 1; // Go to the previous book
               if(bookIndex < 0) // First book wraps to last
               {
                  bookIndex = Enum.GetNames(typeof(BibleBook)).Length - 1;
               }
               book = (BibleBook)bookIndex;
               chapter = int.MaxValue; // Because this gets clamped later
            }

            BibleReference newReference = new BibleReference(oldReference.Version, book, chapter);
            BrowserTab.Selected.GoToReference(ref newReference, BrowserTab.NavigationMode.Add);
            PrintChapter(newReference);

            Debug.WriteLine("Previous chapter called! " + newReference + " gone to from " + oldReference);
         }
      }

      /// <summary>
      /// Display the chapter next to the current one.
      /// </summary>
      private void NextChapter()
      {
         BibleReference oldReference = BrowserTab.Selected.Reference;
         if (oldReference != null) // Not a new tab
         {
            int chapter = oldReference.Chapter;
            BibleBook book = oldReference.Book;

            chapter++;
            if (chapter > oldReference.Chapters.Count)
            {
               int bookIndex = (int)book + 1; // Go to the next book
               if(bookIndex > Enum.GetNames(typeof(BibleBook)).Length - 1) // Last book loops back to first
               {
                  bookIndex = 0;
               }
               book = (BibleBook)bookIndex;
               chapter = 1;
            }

            BibleReference newReference = new BibleReference(oldReference.Version, book, chapter);
            BrowserTab.Selected.GoToReference(ref newReference, BrowserTab.NavigationMode.Add);
            PrintChapter(newReference);
         }
      }

      /// <summary>
      /// Set version, book, and chapter selection boxes to be hidden.
      /// </summary>
      private void HideAllDropdowns()
      {
         if (BrowserTab.Selected != null)
         {
            BibleReference reference = BrowserTab.Selected.Reference;
            if (reference == null)
            {
               asbSearch.PlaceholderText = "Search or enter reference";
            }
            else
            {
               asbSearch.Text = reference.Version + ": " + reference.ToString();
               asbSearch.SelectAll();
            }
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
         if (BrowserTab.Selected != null)
         {
            BibleReference reference = BrowserTab.Selected.Reference;

            if (reference == null)
            {
               ddbVersion.Visibility = Visibility.Collapsed;
               ddbBook.Visibility = Visibility.Collapsed;
               ddbChapter.Visibility = Visibility.Collapsed;
            }
            else
            {
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

               rtbVerses.Focus(FocusState.Programmatic); // Focus away from the search box
            }
         }
      }

      /// <summary>
      /// Get the main text and begin reading it asynchronously.
      /// </summary>
      async private void ReadMainTextAloud()
      {
         if(m_isPlaybackStarted)
         {
            m_mediaElement.Play();
         }
         else
         {
            BibleReference reference = BrowserTab.Selected.Reference;
            string languageCode = reference.Version.Language.ToLower();

            // Detect the voice for the language
            try
            {
               m_synth.Voice = SpeechSynthesizer.AllVoices.Where(p => p.Language.Contains(languageCode)).First();

               // Generate the audio stream from plain text.
               SpeechSynthesisStream stream = await m_synth.SynthesizeTextToStreamAsync(reference.GetChapterPlainText());

               // Send the stream to the media object
               m_mediaElement.SetSource(stream, stream.ContentType);
               m_mediaElement.Play();
               m_isPlaybackStarted = true;
            }
            // The computer doesn't have the language
            catch (InvalidOperationException)
            {
               // Show an error message
               var messageDialog = new MessageDialog("Please install the language pack for this language (" + new CultureInfo(languageCode)  + ").");
               messageDialog.Commands.Add(new UICommand("Close"));
               messageDialog.DefaultCommandIndex = 0;
               messageDialog.CancelCommandIndex = 0;
               await messageDialog.ShowAsync();
            }
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

         cdLeftPadding.Width = new GridLength(coreTitleBar.SystemOverlayLeftInset);
         cdRightPadding.Width = new GridLength(coreTitleBar.SystemOverlayRightInset);

         // Update title bar control size as needed to account for system size changes.
         grdTitleBar.Height = coreTitleBar.Height;
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
      /// <param name="reference">The chapter to print. If null, this will simply erase page contents.</param>
      private void PrintChapter(BibleReference reference)
      {
         EraseText();

         // New tab, leave blank
         if (BrowserTab.Selected.Reference == null)
            return;
         else
            rtbVerses.Blocks.Add(reference.GetChapterTextFormatted());
      }

      private async void PickNewBibleAsync() // TODO
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
      /// Remove the previous chapter contents.
      /// </summary>
      private void EraseText()
      {
         rtbVerses.Blocks.Clear();
      }

      #endregion


      #region Button Events

      private async void Home_Click(object sender, RoutedEventArgs e)
      {
         await BrowserTab.SaveOpenTabs();
      }

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
         PreviousReference();
      }

      /// <summary>
      /// Go to the next reference in the current tab's history.
      /// </summary>
      private void BtnNext_Click(object sender, RoutedEventArgs e)
      {
         NextReference();
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
         asbSearch.Focus(FocusState.Programmatic);
      }

      private void MfiAddBible_Click(object sender, RoutedEventArgs e)
      {
         PickNewBibleAsync();
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

         ActivateButtons();
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
            BibleReference reference = BrowserTab.Selected.Reference;

            if(reference == null)
            {
               asbSearch.Focus(FocusState.Programmatic); // Focus autohides all dropdowns
               PrintChapter(null);
            }
            else
            {
               ShowAllDropdowns();
               PrintChapter(BrowserTab.Selected.Reference);
            }
         }

         ActivateButtons();
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

      #endregion


      #region Search Box Events

      private void AsbSearch_GotFocus(object sender, RoutedEventArgs e)
      {
         HideAllDropdowns();
      }

      private void AsbSearch_LostFocus(object sender, RoutedEventArgs e)
      {
         ShowAllDropdowns();
      }

      private void AsbSearch_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
      {
         if(e.Key == Windows.System.VirtualKey.Enter)
         {
            // Have a reference to go to ready
            BibleReference reference = BrowserTab.Selected.Reference;
            if (reference == null) {
               reference = new BibleReference(BibleVersion.DefaultVersion);
            }

            BibleVersion version = null;
            int bookNumeral = 0;
            string bookName = null;
            int chapter = 0;
            bool foundVersion = false;

            List<string> queryElements = new List<string>();
            string query = ((TextBox)sender).Text;

            // Separate the query into version and reference
            if (query.Contains(':'))
            {
               foundVersion = true;
               queryElements = query.Split(':', StringSplitOptions.RemoveEmptyEntries).ToList();
               version = BibleSearch.VersionByAbbreviation(queryElements[0]);
               if(version == null)
               {
                  version = reference.Version;
                  if(version == null)
                  {
                     version = BibleVersion.DefaultVersion;
                  }
               }
            }

            // Separate the query into book name and verse
            if (foundVersion == true)
            {
               queryElements = queryElements[1].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            else
            {
               version = BibleVersion.DefaultVersion;
               queryElements = query.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            // There must only be a book name
            if (queryElements.Count == 1)
            {
               bookName = BibleSearch.ClosestBookName(version, queryElements[0]);
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
                  reference = new BibleReference(version, reference.Book, chapter);
                  BrowserTab.Selected.GoToReference(ref reference, BrowserTab.NavigationMode.Add);
               }
            }
            else
            {
               // Convert the book to the closest valid book
               bookName = BibleSearch.ClosestBookName(version, bookName);
               if (chapter == 0)
               {
                  reference = new BibleReference(version, BibleReference.StringToBook(bookName, version));
                  BrowserTab.Selected.GoToReference(ref reference, BrowserTab.NavigationMode.Add);
               }
               else
               {
                  reference = new BibleReference(version, BibleReference.StringToBook(bookName, version), chapter);
                  BrowserTab.Selected.GoToReference(ref reference, BrowserTab.NavigationMode.Add);
               }
            }
            ShowAllDropdowns();
            PrintChapter(reference);            
         }
         // Autosuggest
         else
         {
            //string query = asbSearch.Text.Trim();

            //// Autosuggest a version
            //if (!query.Contains(':'))
            //{
            //   foreach(BibleVersion version in BibleLoader.Bibles)
            //   {
            //      if(version.VersionAbbreviation.Length >= query.Length &&
            //         version.VersionAbbreviation.Substring(0, query.Length) == query)
            //      {
            //         // Autosuggest the rest of the version name TODO
            //         asbSearch.Text = version.VersionAbbreviation + ": ";
            //         asbSearch.Select(query.Length, asbSearch.Text.Length - 1);
            //      }
            //   }
            //}
         }
      }

      #endregion


      #region Accelerators

      private void KeyboardAccelerator_PreviousChapter(Windows.UI.Xaml.Input.KeyboardAccelerator sender, Windows.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
      {
         PreviousChapter();
         args.Handled = true;
      }

      private void KeyboardAccelerator_NextChapter(Windows.UI.Xaml.Input.KeyboardAccelerator sender, Windows.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
      {
         NextChapter();
         args.Handled = true;
      }

      private void KeyboardAccelerator_Search(Windows.UI.Xaml.Input.KeyboardAccelerator sender, Windows.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
      {
         asbSearch.Focus(FocusState.Keyboard);
         args.Handled = true;
      }

      #endregion

      private void CbDefaultVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         BibleVersion version = (BibleVersion)e.AddedItems.FirstOrDefault();
         BibleVersion.SetDefaultVersion(version.FileName);
         Debug.WriteLine("Default version setting being set to " + version.FileName);
      }
   }
}