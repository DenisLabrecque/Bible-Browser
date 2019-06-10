using BibleBrowser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BibleBrowserUWP
{
   /// <summary>
   /// An empty page that can be used on its own or navigated to within a Frame.
   /// </summary>
   public sealed partial class MainPage : Page
   {
      #region Member Variables

      // The media object for controlling and playing audio.
      MediaElement m_mediaElement = new MediaElement();

      // The object for controlling the speech synthesis engine (voice).
      SpeechSynthesizer m_synth = new SpeechSynthesizer();
      bool m_isPlaybackStarted = false;

      List<string> contents = new List<string>();

      #endregion


      #region Properties

      ObservableCollection<BrowserTab> Tabs { get => BrowserTab.Tabs; }

      #endregion

      public MainPage()
      {
         this.InitializeComponent();
         StyleTitleBar();

         // Open the tab that was active before the app was last closed
         // Also activates a trigger to load page content
         lvTabs.SelectedIndex = BrowserTab.TabIndex;



         string info = BibleLoader.TestInformation();
         string temp = BibleLoader.TestBookNames();



         //tbMainText.Text = info + temp;

         //tbMainText.Text = new BibleReference(BiblesLoaded.Version("ylt.xml"), BibleBook.iiS, 1, 1);
         //tbMainText.Text = new BibleReference(0, (int)BookShortName.Jb, new ChapterVerse(1, 2, "a"), new ChapterVerse(0)).ToString();
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
               SpeechSynthesisStream stream = await m_synth.SynthesizeTextToStreamAsync(tbMainText.Text);

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
         BrowserTab.SetTabIndex(lvTabs.SelectedIndex);

         // Stop audio playback when changing tabs
         m_isPlaybackStarted = false;
         m_mediaElement.Stop();
         btnPause.Visibility = Visibility.Collapsed;
         btnPlay.Visibility = Visibility.Visible;

         // Only display the reference when there is still a tab to show; if not, we are closing the app anyway
         if (lvTabs.Items.Count > 1)
         {
            BibleReference reference = ((BrowserTab)lvTabs.SelectedItem).Reference;

            // This was a new tab which does not have a reference yet
            if (reference == null)
               tbMainText.Text = string.Empty;
            // Load the tab's reference contents to the page's main area
            else
            {
               ShowBibleTextTemp(reference);
            }
         }
      }

      private void ShowBibleTextTemp(BibleReference reference)
      {
         contents = reference.Version.GetChapterVerses(reference);
         tbMainText.Text = string.Empty;
         foreach (string value in contents)
         {
            tbMainText.Text += value;
         }
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
            Tabs.RemoveAt(0);
            CoreApplication.Exit();
         }
      }


      /// <summary>
      /// Open a new tab.
      /// </summary>
      private void BtnNewTab_Click(object sender, RoutedEventArgs e)
      {
         Tabs.Add(new BrowserTab());
         lvTabs.SelectedIndex = Tabs.Count - 1;
      }


      /// <summary>
      /// Search or find a reference.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="args"></param>
      private void AsbSearch_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
      {
         BibleReference reference = BrowserTab.Selected.Reference;
         if (reference == null)
            reference = new BibleReference(BibleLoader.DefaultVersion, BibleBook.Gn);

         // Find which Bible book the string is closest to by the number of same letters
         float similarity = BibleSearch.LevenshteinSimilarity(reference.BookName.ToLower(), args.QueryText.ToLower());

         tbMainText.Text = "Similarity between " + reference.BookName.ToLower() + " and " + args.QueryText.ToLower() + " is " + similarity;

         string closestBook = BibleSearch.ClosestBookName(reference.Version, args.QueryText);

         tbMainText.Text += "The closest book in " + reference.Version + " is " + closestBook;

         reference.SetBook(closestBook).SetToFirstChapter();
         ShowBibleTextTemp(reference);
      }
      #endregion
   }
}