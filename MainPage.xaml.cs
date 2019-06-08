﻿using BibleBrowser;
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

        ObservableCollection<BrowserTab> m_Tabs { get => BrowserTab.Tabs; }

        // The media object for controlling and playing audio.
        MediaElement m_mediaElement = new MediaElement();

        // The object for controlling the speech synthesis engine (voice).
        SpeechSynthesizer m_synth = new SpeechSynthesizer();
        bool m_isPlaybackStarted = false;

        #endregion

        public MainPage()
        {
            this.InitializeComponent();
            StyleTitleBar();



            string info = BibleLoader.TestInformation();
            string temp = BibleLoader.TestBookNames();
            //tbMainText.Text = info + temp;

            //tbMainText.Text = new BibleReference(BiblesLoaded.Version("ylt.xml"), BibleBook.iiS, 1, 1);
            //tbMainText.Text = new BibleReference(0, (int)BookShortName.Jb, new ChapterVerse(1, 2, "a"), new ChapterVerse(0)).ToString();
        }

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

        #endregion
    }
}
