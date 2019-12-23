using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Devices.Input;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
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
      const int MAXTEXTWIDTH = 400;
      const int VERSECOLUMN = 30;
      const int MIDDLCOLUMN = 30;

      #endregion


      #region Member Variables

      // The media object for controlling and playing audio.
      MediaElement m_mediaElement = new MediaElement();

      // The object for controlling the speech synthesis engine (voice).
      SpeechSynthesizer m_synth = new SpeechSynthesizer();

      bool m_isPlaybackStarted = false;
      bool m_areTabsLoaded = false;
      bool m_isAppNewlyOpened = true;
      bool m_areDropdownsDisplayed = false;
      CancellationTokenSource cancelSearch;
      private BibleReference m_previousReference = new BibleReference(BibleVersion.DefaultVersion, null);
      ObservableCollection<SearchResult> m_SearchResults = new ObservableCollection<SearchResult>();

      #endregion


      #region Properties

      TrulyObservableCollection<BrowserTab> Tabs { get => BrowserTab.Tabs; }
      ObservableCollection<SearchResult> SearchResults { get => m_SearchResults; }

      // Gets all available Bibles, minus the one already selected, if there is one.
      ObservableCollection<BibleVersion> Bibles {
         get {
            if (BrowserTab.Selected == null || BrowserTab.Selected.Reference == null)
               return BibleLoader.Bibles;
            else
               return BrowserTab.Selected.OtherVersions;
         }
      }

      List<Verse> Verses {
         get {
            if (BrowserTab.Selected == null || BrowserTab.Selected.Reference == null)
               return null;
            else
               return BrowserTab.Selected.Reference.Verses;
         }
      }

      /// <summary>
      /// The maximum width the chapter column can take up, in which verses fit.
      /// </summary>
      public double ChapterWidth {
         get; private set;
      }

      /// <summary>
      /// Determine whether the user has a keyboard, or is using touchscreen only (for tablets).
      /// </summary>
      public static bool IsKeyboardAttached {
         get {
            KeyboardCapabilities keyboardCapabilities = new KeyboardCapabilities();
            if(keyboardCapabilities.KeyboardPresent == 0)
            {
               Debug.WriteLine("Keyboard not found.");
               return false;
            }
            else
            {
               Debug.WriteLine("Keyboard present.");
               return true;
            }
         }
      }

      #endregion


      #region Page Initialization and Events

      public MainPage()
      {
         // Load previous tabs when the app opens
         Application.Current.LeavingBackground += new LeavingBackgroundEventHandler(App_LeavingBackground);

         this.InitializeComponent();
         HideAllDropdowns(); // Don't show Genesis 1
         HideSearch();

         // Set theme for window root.
         FrameworkElement root = (FrameworkElement)Window.Current.Content;
         root.RequestedTheme = AppSettings.Theme;
         SetThemeToggle(AppSettings.Theme);
         SetNotificationToggle(AppSettings.ReadingNotifications);
         SetNotificationTime(AppSettings.NotifyTime);

         StyleTitleBar();
         cbDefaultVersion.SelectedItem = BibleVersion.DefaultVersion;
         // Ensure the text remains within the window size
         this.SizeChanged += MainPage_SizeChanged;

         // Save tabs when the app closes
         Application.Current.Suspending += new SuspendingEventHandler(App_Suspending);
      }


      private void SetNotificationTime(TimeSpan notifyTime)
      {
         Debug.WriteLine("Notification time found as " + notifyTime);
         tpNotificationTime.Time = notifyTime;
      }

      /// <summary>
      /// Set the theme toggle to the correct position (off for the default theme, and on for the non-default).
      /// </summary>
      private void SetThemeToggle(ElementTheme theme)
      {
         if (theme == AppSettings.DEFAULTTHEME)
            tglAppTheme.IsOn = false;
         else
            tglAppTheme.IsOn = true;
      }

      /// <summary>
      /// Set the Bible reading notification toggle.
      /// </summary>
      private void SetNotificationToggle(bool notificationsAllowed)
      {
         if(notificationsAllowed)
         {
            tglNotifications.IsOn = true;
            tpNotificationTime.IsEnabled = true;
         }
         else
         {
            tglNotifications.IsOn = false;
            tpNotificationTime.IsEnabled = false;
         }
      }

      /// <summary>
      /// Fires when the window size is changed by dragging, snapping, or pixel density.
      /// </summary>
      private void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
      {
         SetWidth();
      }

      /// <summary>
      /// Set the width of the title bar and reading main content area.
      /// </summary>
      private void SetWidth()
      {
         double pageWidth = ((Frame)Window.Current.Content).ActualWidth;
         double contentWidth;

         // Do the same for the compare view
         if (pageWidth < (2 * MINMARGIN) + (2 * MAXTEXTWIDTH))
         {
            contentWidth = pageWidth - (2 * MINMARGIN) - VERSECOLUMN - MIDDLCOLUMN;
            ChapterWidth = contentWidth;
            gvCompareVerses.Width = contentWidth;
         }
         else
         {
            contentWidth = (MAXTEXTWIDTH * 2) - VERSECOLUMN - MIDDLCOLUMN;
            ChapterWidth = contentWidth;
            gvCompareVerses.Width = contentWidth;
         }

         // Set the maximum width of the tab area
         CoreApplicationViewTitleBar titleBar = CoreApplication.GetCurrentView().TitleBar;
         spTabArea.MaxWidth = pageWidth - (titleBar.SystemOverlayLeftInset + titleBar.SystemOverlayRightInset);
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

         SetWidth();
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
            grdTitleBar.Visibility = Visibility.Visible;
         else
            grdTitleBar.Visibility = Visibility.Collapsed;
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
            reference.VerticalScrollOffset = svPageScroller.VerticalOffset;
            BrowserTab.Selected.GoToReference(ref reference, BrowserTab.NavigationMode.Previous);
            PrintChapter(reference);
            ActivateButtons();
            svPageScroller.ChangeView(null, reference.VerticalScrollOffset, null);
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
            reference.VerticalScrollOffset = svPageScroller.VerticalOffset;
            BrowserTab.Selected.GoToReference(ref reference, BrowserTab.NavigationMode.Next);
            PrintChapter(reference);
            ActivateButtons();
            svPageScroller.ChangeView(null, reference.VerticalScrollOffset, null);
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

            BibleReference newReference = new BibleReference(oldReference.Version, oldReference.ComparisonVersion, book, chapter);
            BrowserTab.Selected.GoToReference(ref newReference, BrowserTab.NavigationMode.Add);
            svPageScroller.ChangeView(null, 0, null, true);
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

            BibleReference newReference = new BibleReference(oldReference.Version, oldReference.ComparisonVersion, book, chapter);
            BrowserTab.Selected.GoToReference(ref newReference, BrowserTab.NavigationMode.Add);
            svPageScroller.ChangeView(null, 0, null, true);
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
               //btnCompare.IsEnabled = false;
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
         m_areDropdownsDisplayed = false;
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
               //btnCompare.IsEnabled = false;

               gvBooks.ItemsSource = m_previousReference.Version.BookNames;
               lvChapters.ItemsSource = m_previousReference.Chapters;

               asbSearch.Text = string.Empty;
               asbSearch.PlaceholderText = string.Empty;

               ddbVersion.Visibility = Visibility.Visible;
               ddbBook.Visibility = Visibility.Visible;
               ddbChapter.Visibility = Visibility.Collapsed;
               m_areDropdownsDisplayed = true;
            }
            else
            {
               // Fill dropdowns with content
               gvBooks.ItemsSource = reference.Version.BookNames;
               lvChapters.ItemsSource = reference.Chapters;

               asbSearch.Text = string.Empty;
               asbSearch.PlaceholderText = string.Empty;

               ddbVersion.Content = reference.Version;
               ddbBook.Content = reference.BookName;
               ddbChapter.Content = reference.Chapter;

               ddbVersion.Visibility = Visibility.Visible;
               ddbBook.Visibility = Visibility.Visible;
               ddbChapter.Visibility = Visibility.Visible;

               txtSearchStatus.Focus(FocusState.Programmatic); // Focus away from the search box
               m_areDropdownsDisplayed = true;
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
         appBar.ButtonForegroundColor = Colors.White;
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
            //btnPlay.IsEnabled = false;
         }
         // There is text already displayed: check whether there is history
         else
         {
            //btnPlay.IsEnabled = true;

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
         // New tab, leave blank
         if (BrowserTab.Selected.Reference == null)
         {
            gvCompareVerses.ItemsSource = null;
         }
         // Single version
         else if (reference.ComparisonVersion == null)
         {
            gvCompareVerses.ItemsSource = null;
            gvCompareVerses.ItemsSource = reference.Verses;
         }
         // With comparison version
         else
         {
            gvCompareVerses.ItemsSource = null;
            gvCompareVerses.ItemsSource = reference.Verses;
         }
      }

      /// <summary>
      /// TODO
      /// </summary>
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
      /// Add a version to compare to the original reference, and print the new layout.
      /// </summary>
      private void AddCompareToVersion(BibleVersion compareVersion, BibleReference oldReference)
      {
         BibleReference newReference = new BibleReference(oldReference.Version, compareVersion, oldReference.Book, oldReference.Chapter, oldReference.Verse);
         BrowserTab.Selected.GoToReference(ref newReference, BrowserTab.NavigationMode.Add);
         Debug.WriteLine("Compare version added as " + newReference.ComparisonVersion);
      }

      /// <summary>
      /// Remove a version to compare to the original reference, and print the new layout.
      /// </summary>
      private void RemoveCompareToVersion(BibleReference oldReference)
      {
         BibleReference newReference = new BibleReference(oldReference.Version, oldReference.ComparisonVersion, oldReference.Book, oldReference.Chapter, oldReference.Verse);
         BrowserTab.Selected.GoToReference(ref newReference, BrowserTab.NavigationMode.Add);
         Debug.WriteLine("Compare version removed.");
      }
      
      #endregion


      #region Events

      private async void Home_Click(object sender, RoutedEventArgs e)
      {
         await BrowserTab.SaveOpenTabs();
      }

      private void BtnPlay_Click(object sender, RoutedEventArgs e)
      {
         ReadMainTextAloud();
         //btnPlay.Visibility = Visibility.Collapsed;
         //btnPause.Visibility = Visibility.Visible;
      }

      private void BtnPause_Click(object sender, RoutedEventArgs e)
      {
         m_mediaElement.Pause();
         //btnPause.Visibility = Visibility.Collapsed;
         //btnPlay.Visibility = Visibility.Visible;
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
         if(BrowserTab.Selected != null && BrowserTab.Selected.Reference != null)
            m_previousReference = BrowserTab.Selected.Reference;
         Tabs.Add(new BrowserTab());
         //btnCompare.IsEnabled = false;
         lvTabs.SelectedIndex = Tabs.Count - 1;
         asbSearch.Text = string.Empty;
         ActivateButtons();
         ShowAllDropdowns();

         //if(IsKeyboardAttached)
         //   asbSearch.Focus(FocusState.Programmatic); // TODO this seem to always execute
         //                                                  regardless of being in tablet mode without keyboard
      }

      private void MfiAddBible_Click(object sender, RoutedEventArgs e)
      {
         PickNewBibleAsync();
      }

      /// <summary>
      /// Close a tab.
      /// </summary>
      private async void BtnCloseTab_Click(object sender, RoutedEventArgs e)
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
            await BrowserTab.SaveOpenTabs();
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
         //btnPause.Visibility = Visibility.Collapsed;
         //btnPlay.Visibility = Visibility.Visible;

         // Only display the reference when there is still a tab to show; if not, we are closing the app anyway
         if (lvTabs.Items.Count > 0)
         {
            BibleReference reference = BrowserTab.Selected.Reference;

            if(reference == null)
            {
               //asbSearch.Focus(FocusState.Programmatic); // Focus autohides all dropdowns // TODO this causes problems on tablets
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
      /// When a version is selected from the "compare to" list.
      /// </summary>
      private void LvCompareVersions_ItemClicked(object sender, ItemClickEventArgs e)
      {
         AddCompareToVersion((BibleVersion)e.ClickedItem, BrowserTab.Selected.Reference);
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
         BibleReference newReference;
         if (version == oldReference.ComparisonVersion) // Flip versions when they would result in two of the same version
         {
            newReference = new BibleReference(version, oldReference.Version, oldReference.Book, oldReference.Chapter);
         }
         else {
            newReference = new BibleReference(version, oldReference.ComparisonVersion, oldReference.Book, oldReference.Chapter);
         }
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
         BibleVersion version;
         BibleVersion comparisonVersion;
         BibleReference reference;
         if (BrowserTab.Selected.Reference != null) // This tab is already open
         {
            version = BrowserTab.Selected.Reference.Version;
            comparisonVersion = BrowserTab.Selected.Reference.ComparisonVersion;
            reference = new BibleReference(version, comparisonVersion, BibleReference.StringToBook(book, version));
         }
         // A new tab has a null reference, but the user may be seeing dropdowns relating to the previous reference;
         // this is desirable because it gives him a default starting point for his new tab when using the touchscreen.
         else
         {
            reference = m_previousReference;
         }
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
         BibleReference newReference = new BibleReference(oldReference.Version, oldReference.ComparisonVersion, oldReference.Book, chapter);
         BrowserTab.Selected.GoToReference(ref newReference, BrowserTab.NavigationMode.Add);

         flyChapter.Hide();
      }

      #endregion


      #region Search Box Events

      private void AsbSearch_GotFocus(object sender, RoutedEventArgs e)
      {
         if(m_isAppNewlyOpened)
         {
            // Ignore the first interaction so that the search bar doesn't get focus on app opening
            m_isAppNewlyOpened = false;
         }
         else
         {
            HideAllDropdowns();
         }
      }

      private void AsbSearch_LostFocus(object sender, RoutedEventArgs e)
      {
         ShowAllDropdowns();
      }

      /// <summary>
      /// Called every time search reports progress.
      /// Put code that manages new search results in here.
      /// https://devblogs.microsoft.com/dotnet/async-in-4-5-enabling-progress-and-cancellation-in-async-apis/
      /// </summary>
      private void ReportSearchProgress(SearchProgressInfo progress)
      {
         // Update the UI to reflect the progress value that is passed back.
         Debug.WriteLine("Progress reported from search with values: " + " task: " + progress.Status + ", percent: " + progress.Completion * 100);
         progSearchProgress.Value = progress.Completion;
         lvSearchResults.ItemsSource = m_SearchResults;
         if(m_SearchResults.Count < progress.Results.Count) // A new result was found since last time
         {
            // Add the new results that were found
            for (int i = Math.Clamp(m_SearchResults.Count - 1, 0, int.MaxValue); i < Math.Clamp(progress.Results.Count - 1, 0, int.MaxValue); i++)
            {
               m_SearchResults.Add(progress.Results[i]);
            }
         }
         txtSearchStatus.Text = progress.Status;
      }

      private void HideSearch()
      {
         lvSearchResults.Visibility = Visibility.Collapsed;
         progSearchProgress.Visibility = Visibility.Collapsed;
         txtSearchStatus.Visibility = Visibility.Collapsed;
         btnCancelSearch.Visibility = Visibility.Collapsed;
      }

      /// <summary>
      /// Show the search results region and progress bar.
      /// </summary>
      private void ShowSearch()
      {
         lvSearchResults.Visibility = Visibility.Visible;
         progSearchProgress.Visibility = Visibility.Visible;
         txtSearchStatus.Visibility = Visibility.Visible;
         btnCancelSearch.Visibility = Visibility.Visible;
      }

      /// <summary>
      /// Detect different keystrokes in the search bar.
      /// </summary>
      private async void AsbSearch_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
      {
         if(e.Key == Windows.System.VirtualKey.Enter)
         {
            string query = ((TextBox)sender).Text;
            //QueryIsInJumpToForm();

            // Have a reference to go to ready
            BibleReference reference = BrowserTab.Selected.Reference;
            if (reference == null) {
               reference = new BibleReference(BibleVersion.DefaultVersion, null);
            }

            BibleVersion version = null;
            int bookNumeral = 0;
            string bookName = null;
            int chapter = 0;
            bool foundVersion = false;

            List<string> queryElements = new List<string>();




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
            else
            {
               version = BibleVersion.DefaultVersion;
            }


            //// TESTING
            /// try searching for this in the Bible
            if (version == null)
            {
               throw new Exception("Version null");
            }
            else
            {
               // Construct Progress<T>, passing ReportProgress as the Action<T> 
               Progress<SearchProgressInfo> progressIndicator = new Progress<SearchProgressInfo>(ReportSearchProgress);
               cancelSearch = new CancellationTokenSource();
               // Call async method
               ShowSearch();
               m_SearchResults.Clear(); // Empty from any previous search results
               SearchProgressInfo task = await BibleSearch.SearchAsync(version, query, progressIndicator, cancelSearch.Token);
               // Handle the search being cancelled at any point
               if (task.IsCanceled)
               {
                  HideSearch();
                  lvSearchResults.ItemsSource = null;
               }
               else
               {
                  // Show the number of results as status
                  if(m_SearchResults.Count == 0)
                  {
                     txtSearchStatus.Text = "No result for '" + task.Query + "'";
                  }
                  else if(m_SearchResults.Count == 1)
                  {
                     txtSearchStatus.Text = "One result for '" + task.Query + "'";
                  }
                  else
                  {
                     txtSearchStatus.Text = task.Results.Count + " results for '" + task.Query + "'";
                  }
               }
               cancelSearch.Dispose();
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
               bookName = BibleSearch.ClosestBookName(version, queryElements[0], out _);
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
                  reference = new BibleReference(version, null, reference.Book, chapter);
                  BrowserTab.Selected.GoToReference(ref reference, BrowserTab.NavigationMode.Add);
               }
            }
            else
            {
               // Convert the book to the closest valid book
               bookName = BibleSearch.ClosestBookName(version, bookName, out _);
               if (chapter == 0)
               {
                  reference = new BibleReference(version, null, BibleReference.StringToBook(bookName, version));
                  BrowserTab.Selected.GoToReference(ref reference, BrowserTab.NavigationMode.Add);
               }
               else
               {
                  reference = new BibleReference(version, null, BibleReference.StringToBook(bookName, version), chapter);
                  BrowserTab.Selected.GoToReference(ref reference, BrowserTab.NavigationMode.Add);
               }
            }
            ShowAllDropdowns();
            PrintChapter(reference);            
         }
         // Select the search box text on backspace if the dropdowns are being displayed
         else if(e.Key == Windows.System.VirtualKey.Back)
         {
            if (m_areDropdownsDisplayed)
            {
               HideAllDropdowns();
            }
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

      /// <summary>
      /// Test whether the query closely matches a Bible book name.
      /// When this is so, the whole query should be formatted in such a way that it fits the pattern.
      /// Two ways the pattern can match:
      /// 1. The query uses a correct version abbreviation (eg. KJV), followed by a colon, followed by a correct or nearly correct
      /// book name, followed by optional numbers only.
      /// 2. The query uses a correct or nearly correct book name, followed by optional numbers.
      /// </summary>
      private void QueryIsInJumpToForm()
      {

      }

      #endregion


      #region Accelerators

      private void KeyboardAccelerator_Search(Windows.UI.Xaml.Input.KeyboardAccelerator sender, Windows.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
      {
         asbSearch.Focus(FocusState.Keyboard);
         args.Handled = true;
      }

      #endregion

      private void CbDefaultVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         BibleVersion defaultVersion = (BibleVersion)e.AddedItems.FirstOrDefault();
         BibleVersion.SetDefaultVersion(defaultVersion.FileName);
         Debug.WriteLine("Default version setting being set to " + defaultVersion.FileName);
      }

      private void DdbVersion_Click(object sender, RoutedEventArgs e)
      {
         // Refresh the list
         //gvVersions.ItemsSource = null;
         //gvVersions.ItemsSource = Bibles;
      }


      /// <summary>
      /// Switch the app's theme between light mode and dark mode, and save that setting.
      /// </summary>
      private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
      {
         FrameworkElement window = (FrameworkElement)Window.Current.Content;

         if (((ToggleSwitch)sender).IsOn)
         {
            AppSettings.Theme = AppSettings.NONDEFLTHEME;
            window.RequestedTheme = AppSettings.NONDEFLTHEME;
         }
         else
         {
            AppSettings.Theme = AppSettings.DEFAULTTHEME;
            window.RequestedTheme = AppSettings.DEFAULTTHEME;
         }
      }

      private void BtnLeftPage_Click(object sender, RoutedEventArgs e)
      {
         PreviousChapter();
      }

      private void BtnRightPage_Click(object sender, RoutedEventArgs e)
      {
         NextChapter();
      }

      private void SwipeItem_PreviousChapter(SwipeItem sender, SwipeItemInvokedEventArgs args)
      {
         Debug.WriteLine("Previous chapter invoked.");
         PreviousChapter();
         Debug.WriteLine("Previous chapter invoke ended");
      }

      private void SwipeItem_NextChapter(SwipeItem sender, SwipeItemInvokedEventArgs args)
      {
         Debug.WriteLine("Next chapter invoked");
         NextChapter();
         Debug.WriteLine("Next chapter ended");
      }

      private void BtnRemoveCompareView_Click(object sender, RoutedEventArgs e)
      {
         RemoveCompareToVersion(BrowserTab.Selected.Reference);
      }

      private void BtnCancelSearch_Click(object sender, RoutedEventArgs e)
      {
         try
         {
            cancelSearch.Cancel();
         }
         // The search is finished, so it can no longer be cancelled
         catch(ObjectDisposedException)
         {
            // Close the search bar
            HideSearch();
         }
      }

      /// <summary>
      /// Toggle toast notification remeinders every day for doing your Bible reading.
      /// </summary>
      private void TglNotifications_Toggled(object sender, RoutedEventArgs e)
      {
         if (((ToggleSwitch)sender).IsOn)
         {
            AppSettings.ReadingNotifications = !AppSettings.NONOTIFICATIONS;
            tpNotificationTime.IsEnabled = true;
            Debug.WriteLine("Toast notifications " + AppSettings.ReadingNotifications);
         }
         else
         {
            AppSettings.ReadingNotifications = AppSettings.NONOTIFICATIONS;
            tpNotificationTime.IsEnabled = false;
            Debug.WriteLine("Toast notifications " + AppSettings.ReadingNotifications);
         }
      }

      private void TpNotificationTime_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
      {
         AppSettings.NotifyTime = e.NewTime;
         Debug.WriteLine("Time changed to " + AppSettings.NotifyTime);
      }
   }
}