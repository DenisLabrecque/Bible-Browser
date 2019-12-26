﻿using System;
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
using Microsoft.Toolkit.Uwp.Notifications; // Notifications library
using Windows.UI.Notifications;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BibleBrowserUWP
{
   enum CurrentView { Chapter, Search }

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

      CurrentView m_currentView = CurrentView.Chapter;
      bool m_wasSearchResultClicked = false;
      bool m_isPlaybackStarted = false;
      bool m_areTabsLoaded = false;
      bool m_isAppNewlyOpened = true;
      bool m_areDropdownsDisplayed = false;
      CancellationTokenSource m_cancelSearch;
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
         ShowSearch(false);

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
         //tpNotificationTime.Time = notifyTime;
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
            //tglNotifications.IsOn = true;
            //tpNotificationTime.IsEnabled = true;
         }
         else
         {
            //tglNotifications.IsOn = false;
            //tpNotificationTime.IsEnabled = false;
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
            lvSearchResults.Width = contentWidth;
         }
         else
         {
            contentWidth = (MAXTEXTWIDTH * 2) - VERSECOLUMN - MIDDLCOLUMN;
            ChapterWidth = contentWidth;
            gvCompareVerses.Width = contentWidth;
            lvSearchResults.Width = contentWidth;
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
      private void GoToPreviousReference()
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
      private void GoToNextReference()
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
               asbSearch.PlaceholderText = "Search or enter reference";
            }
            else if(reference.ComparisonVersion == null)
            {
               asbSearch.Text = reference.Version + ": " + reference.ToString();
               asbSearch.SelectAll();
            }
            else
            {
               asbSearch.Text = reference.Version + ":" + reference.ComparisonVersion + " " + reference.ToString();
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

               if (reference.ComparisonVersion == null)
               {
                  btnRemoveCompareView.Visibility = Visibility.Collapsed;
                  ddbVersion.Content = reference.Version;
               }
               else
               {
                  btnRemoveCompareView.Visibility = Visibility.Visible;
                  ddbVersion.Content = reference.Version + ":" + reference.ComparisonVersion;
               }
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
      /// Stop a search if it is in progress.
      /// </summary>
      /// <param name="reference">The chapter to print. If null, this will simply erase page contents.</param>
      private void PrintChapter(BibleReference reference)
      {
         ShowSearch(false);

         lvSearchResults.ItemsSource = null;
         if(m_cancelSearch != null)
            m_cancelSearch.Dispose();

         ShowChapter(true);

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

      private void SetCurrentView(CurrentView view)
      {
         switch(view)
         {
            case CurrentView.Chapter:
               ShowChapter(true);
               ShowSearch(false);
               break;
            case CurrentView.Search:
               ShowChapter(false);
               ShowSearch(true);
               break;
            default:
               break;
         }
      }


      /// <summary>
      /// Show the chapter text. Does not print anything.
      /// </summary>
      private void ShowChapter(bool show)
      {
         if (show)
         {
            gvCompareVerses.Visibility = Visibility.Visible;
            btnLeftPage.Visibility = Visibility.Visible;
            btnRightPage.Visibility = Visibility.Visible;
         }
         else
         {
            gvCompareVerses.Visibility = Visibility.Collapsed;
            btnLeftPage.Visibility = Visibility.Collapsed;
            btnRightPage.Visibility = Visibility.Collapsed;
         }
      }


      /// <summary>
      /// Show the search results region and progress bar.
      /// </summary>
      private void ShowSearch(bool show)
      {
         if (show)
         {
            lvSearchResults.Visibility = Visibility.Visible;
            progSearchProgress.Visibility = Visibility.Visible;
            txtSearchStatus.Visibility = Visibility.Visible;
            btnCancelSearch.Visibility = Visibility.Visible;
         }
         else
         {
            lvSearchResults.Visibility = Visibility.Collapsed;
            progSearchProgress.Visibility = Visibility.Collapsed;
            txtSearchStatus.Visibility = Visibility.Collapsed;
            btnCancelSearch.Visibility = Visibility.Collapsed;
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
         if (oldReference.ComparisonVersion != null)
         {
            BibleReference newReference = new BibleReference(oldReference.Version, null, oldReference.Book, oldReference.Chapter, oldReference.Verse);
            BrowserTab.Selected.GoToReference(ref newReference, BrowserTab.NavigationMode.Add);
            Debug.WriteLine("Compare version removed.");
         }
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
      /// Go to the previous search result if that is the case.
      /// </summary>
      private void BtnPrevious_Click(object sender, RoutedEventArgs e)
      {
         if(m_wasSearchResultClicked == true)
         {
            m_wasSearchResultClicked = false;
            ShowSearch(true);
         }
         else
            GoToPreviousReference();
      }

      /// <summary>
      /// Go to the next reference in the current tab's history.
      /// </summary>
      private void BtnNext_Click(object sender, RoutedEventArgs e)
      {
         GoToNextReference();
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
            HideAllDropdowns();
      }

      private void AsbSearch_LostFocus(object sender, RoutedEventArgs e)
      {
         if (asbSearch.Text == string.Empty)
         {
            ShowAllDropdowns();
            ShowChapter(true);
         }
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
            for (int i = Math.Clamp(m_SearchResults.Count, 0, int.MaxValue); i < Math.Clamp(progress.Results.Count, 0, int.MaxValue); i++)
            {
               m_SearchResults.Add(progress.Results[i]);
            }
         }
         txtSearchStatus.Text = progress.Status;
      }

      /// <summary>
      /// Detect different keystrokes in the search bar.
      /// </summary>
      private async void AsbSearch_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
      {
         if(e.Key == Windows.System.VirtualKey.Enter)
         {
            m_SearchResults = new ObservableCollection<SearchResult>(); // Erase previous search results
            string query = ((TextBox)sender).Text;
            query = query.Trim().RemoveDiacritics();
            if (string.IsNullOrWhiteSpace(query))
               return;

            List<string> splitQuery = query.Split(new char[] { ' ', ':', ';', '.', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            BibleVersion version = BrowserTab.Selected.Reference.Version;
            BibleVersion comparison = BrowserTab.Selected.Reference.ComparisonVersion;
            string book = BrowserTab.Selected.Reference.BookName;
            int chapter = 1;

            // The whole query is surrounded by quotes
            if(BibleSearch.QuerySurroundedByQuotes(ref splitQuery))
            {
               string search = BibleSearch.ReassembleSplitString(splitQuery, true);
               Search(search, version);
            }
            // The query has a version prefix (like KJV), so it is treated as a go to reference
            else if(BibleSearch.QueryHasBibleVersion(ref splitQuery, ref version, ref comparison))
            {
               // There is additional information
               if (splitQuery.Count > 0)
               {
                  if (BibleSearch.QuerySurroundedByQuotes(ref splitQuery))
                  {
                     string search = BibleSearch.ReassembleSplitString(splitQuery, true);
                     Search(search, version);
                  }
                  else
                  {
                     BibleSearch.QueryHasBibleBook(ref splitQuery, version, ref book);
                     BibleSearch.QueryHasChapter(ref splitQuery, ref chapter);

                     BibleReference newReference = new BibleReference(version, comparison, book, chapter);
                     BrowserTab.Selected.GoToReference(ref newReference, BrowserTab.NavigationMode.Add);
                     ShowAllDropdowns();
                  }
               }
               // A version prefix like KJV only
               else
               {
                  if(version != BrowserTab.Selected.Reference.Version || comparison != BrowserTab.Selected.Reference.ComparisonVersion)
                  {
                     BibleReference newReference = new BibleReference(version, comparison, BrowserTab.Selected.Reference.Book, BrowserTab.Selected.Reference.Chapter);
                     BrowserTab.Selected.GoToReference(ref newReference, BrowserTab.NavigationMode.Add);
                     ShowAllDropdowns();
                  }
               }
            }
            // The query does not have a version prefix, so decide whether it is a go to reference or a search
            else
            {
               float similarity = BibleSearch.QueryHasBibleBook(ref splitQuery, version, ref book);

               // This is a reference for sure
               if (similarity > 0.25f)
               {
                  BibleSearch.QueryHasChapter(ref splitQuery, ref chapter);
                  BibleReference newReference = new BibleReference(version, comparison, book, chapter);
                  BrowserTab.Selected.GoToReference(ref newReference, BrowserTab.NavigationMode.Add);
                  ShowAllDropdowns();
               }
               // Not sure whether this is a search or go to. Show both.
               else if(0.25f > similarity && similarity > 0.1f)
               {
                  Search(query, version);
               }
               // This is a search
               else
               {
                  Search(query, version);
               }
            }
         }
         // Select the search box text on backspace if the dropdowns are being displayed
         else if(e.Key == Windows.System.VirtualKey.Back)
         {
            if (m_areDropdownsDisplayed)
            {
               HideAllDropdowns();
            }

            CancelSearch();
         }
         // Was a letter; erase the chapter
         else
         {
            ShowChapter(true);
         }
      }


      /// <summary>
      /// Start a Bible search, doing all the things that need to be done in the layout as well.
      /// </summary>
      private void Search(string query, BibleVersion version)
      {
         ShowChapter(false);
         SearchAsync(query, version);
      }


      /// <summary>
      /// Start a search for a particular phrase.
      /// </summary>
      /// <param name="query">The non-diacritic, simplified query the user has typed.</param>
      /// <param name="version">The version of the Bible to search in.</param>
      /// <returns></returns>
      private async Task SearchAsync(string query, BibleVersion version)
      {
         if (version == null)
         {
            throw new Exception("Search version null");
         }
         else
         {
            if (m_cancelSearch != null)
               m_cancelSearch.Dispose();

            // Construct Progress<T>, passing ReportProgress as the Action<T> 
            Progress<SearchProgressInfo> progressIndicator = new Progress<SearchProgressInfo>(ReportSearchProgress);
            m_cancelSearch = new CancellationTokenSource();
            // Call async method
            ShowSearch(true);
            m_SearchResults.Clear(); // Empty from any previous search results
            SearchProgressInfo task = await BibleSearch.SearchAsync(version, query, progressIndicator, m_cancelSearch.Token);
            // Handle the search being cancelled at any point
            if (task.IsCanceled)
            {
               StopSearch();
            }
            else
            {
               var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

               // Show the number of results as status
               if (m_SearchResults.Count == 0)
               {
                  txtSearchStatus.Text = loader.GetString("noResultFor") + " '" + task.Query + "'";
               }
               else if (m_SearchResults.Count == 1)
               {
                  txtSearchStatus.Text = loader.GetString("oneResultFor") + " '" + task.Query + "'";
               }
               else
               {
                  txtSearchStatus.Text = task.Results.Count + " " + loader.GetString("manyResultsFor") + " '" + task.Query + "'";
               }
            }
            m_cancelSearch.Dispose();
         }
      }

      private void StopSearch()
      {
         ShowSearch(false);
         lvSearchResults.ItemsSource = null;
         m_cancelSearch.Dispose();
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
         CancelSearch();
      }

      private void CancelSearch()
      {
         try
         {
            m_cancelSearch.Cancel();
         }
         catch (NullReferenceException) { }
         // The search is finished, so it can no longer be cancelled
         catch (ObjectDisposedException)
         {
            // Close the search bar
            ShowSearch(false);
         }
      }

      /// <summary>
      /// Toggle toast notification remeinders every day for doing your Bible reading.
      /// </summary>
      private void TglNotifications_Toggled(object sender, RoutedEventArgs e)
      {
         // Notifications turned on
         if (((ToggleSwitch)sender).IsOn)
         {
            AppSettings.ReadingNotifications = !AppSettings.NONOTIFICATIONS;
            //tpNotificationTime.IsEnabled = true;
            ConstructReminderToast(new DateTimeOffset(DateTime.Now.AddSeconds(2))); // TODO this should create a schedule
            //ConstructReminderToast(new DateTimeOffset(DateTime.Today.AddDays(1).AddHours(AppSettings.NotifyTime.Hours).AddMinutes(AppSettings.NotifyTime.Minutes)));
            Debug.WriteLine("Toast notifications " + AppSettings.ReadingNotifications);
         }
         // Notifications turned off
         else
         {
            AppSettings.ReadingNotifications = AppSettings.NONOTIFICATIONS;
            //tpNotificationTime.IsEnabled = false;
            Debug.WriteLine("Toast notifications " + AppSettings.ReadingNotifications);
         }
      }

      /// <summary>
      /// Save the new notification time.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void TpNotificationTime_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
      {
         AppSettings.NotifyTime = e.NewTime;
         Debug.WriteLine("Time changed to " + AppSettings.NotifyTime);
      }

      private void ConstructReminderToast(DateTimeOffset time)
      {
         var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
         string title = loader.GetString("bibleReading");
         string content = loader.GetString("rememberBibleReading");

         // Create the visual toast elements
         ToastVisual visual = new ToastVisual()
         {
            BindingGeneric = new ToastBindingGeneric()
            {
               Children =
               {
                  new AdaptiveText() { Text = title },
                  new AdaptiveText() { Text = content }
               }
            }
         };

         // Put together the toast to push
         ToastContent toastContent = new ToastContent()
         {
            Visual = visual
         };

         // Create the toast notification object.
         ScheduledToastNotification toast = new ScheduledToastNotification(toastContent.GetXml(), time);
         ToastNotificationManager.CreateToastNotifier().AddToSchedule(toast);

         //// Create the actual toast notification
         //ToastNotification toast = new ToastNotification(toastContent.GetXml());
         //toast.ExpirationTime = DateTime.Now.AddHours(12);
         //ToastNotificationManager.CreateToastNotifier().Show(toast);
      }

      private void LvSearchResults_ItemClick(object sender, ItemClickEventArgs e)
      {
         m_wasSearchResultClicked = true;
         BibleReference reference = ((SearchResult)e.ClickedItem).Reference;
         Debug.WriteLine("Search to go to reference: " + reference);
         BrowserTab.Selected.GoToReference(ref reference);
         PrintChapter(reference);
      }
   }
}