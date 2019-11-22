using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x412에 나와 있습니다.

namespace App1
{
    public sealed partial class MainPage : Page
    {
        MediaCapture mediaCapture;
        bool isPreviewing;
        bool isShot;
        bool isDemo;
        DisplayRequest displayRequest = new DisplayRequest();
        int[][] modeMap = new int[4][]
        {
            new int[2] { 1, 2 },
            new int[2] { 0, 2 },
            new int[2] { 0, 3 },
            new int[2] { 0, 2 }
        };
        int nowModeNum;

        public MainPage()
        {
            this.InitializeComponent();

            ApplicationView.PreferredLaunchViewSize = new Size(700, 700);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            isShot = false;
            isDemo = true;
            Go(0);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await StartPreviewAsync().ConfigureAwait(true);
        }

        private async Task StartPreviewAsync()
        {
            try
            {
                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync();

                displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            }
            catch (UnauthorizedAccessException)
            {
                // This will be thrown if the user denied access to the camera in privacy settings
                MsgBox("Fail to InitializeAsync()", "Check First Try", "OK");
                //ShowMessageToUser("The app was denied access to the camera");
                return;
            }

            try
            {
                PreviewControl.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
                isPreviewing = true;
            }
            catch (System.IO.FileLoadException)
            {
                mediaCapture.CaptureDeviceExclusiveControlStatusChanged += _mediaCapture_CaptureDeviceExclusiveControlStatusChanged;
            }
        }

        private async void _mediaCapture_CaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
            if (args.Status == MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
            {
                MsgBox("Access denined", "Check Second Catch", "OK");
                //ShowMessageToUser("The camera preview can't be displayed because another app has exclusive access");
            }
            else if (args.Status == MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable && !isPreviewing)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await StartPreviewAsync().ConfigureAwait(true);
                });
            }
        }

        async void MsgBox(String msg_title, String msg_content, String msg_btnContent)
        {
            var msgBoxDlg = new MessageDialog(msg_content, msg_title);
            msgBoxDlg.Commands.Add(new Windows.UI.Popups.UICommand(msg_btnContent) { Id = 0 });
            msgBoxDlg.DefaultCommandIndex = 0;
            await msgBoxDlg.ShowAsync();
        }

        // Button 0
        private void btn_demo_Click(object sender, RoutedEventArgs e)
        {
            Go(modeMap[nowModeNum][0]);
        }

        // Button 1
        private void btn_shot_Click(object sender, RoutedEventArgs e)
        {
            Go(modeMap[nowModeNum][1]);
        }

        void AllCollapsed()
        {
            PreviewControl.Visibility = Visibility.Collapsed;
            img_left.Visibility = Visibility.Collapsed;
            img_right.Visibility = Visibility.Collapsed;
        }

        protected async void Go(int modeNum)
        {
            AllCollapsed();
            switch (modeNum)
            {
                case 0:
                    nowModeNum = 0;
                    img_left.Source = new BitmapImage(new Uri(this.BaseUri, "Assets/imgs/demo_person.jpg"));
                    img_left.Visibility = Visibility.Visible;
                    isShot = false;
                    isDemo = true;
                    combo_go_SelectionChanged(null, null);
                    img_right.Visibility = Visibility.Visible;
                    break;
                case 1:
                    nowModeNum = 1;
                    img_left.Source = new BitmapImage(new Uri(this.BaseUri, "Assets/imgs/demo_person.jpg"));
                    img_left.Visibility = Visibility.Visible;
                    isShot = true;
                    isDemo = true;
                    combo_go_SelectionChanged(null, null);
                    img_right.Visibility = Visibility.Visible;
                    break;
                case 2:
                    nowModeNum = 2;
                    PreviewControl.Visibility = Visibility.Visible;
                    isShot = false;
                    isDemo = false;
                    combo_go_SelectionChanged(null, null);
                    img_right.Visibility = Visibility.Visible;
                    break;
                case 3:
                    nowModeNum = 3;
                    await TakePicture().ConfigureAwait(true);
                    StorageFile photoFile = await KnownFolders.PicturesLibrary.GetFileAsync("photo.jpg");
                    using (IRandomAccessStream photoStream = await photoFile.OpenAsync(FileAccessMode.Read))
                    {
                        BitmapImage photoImage = new BitmapImage();
                        photoImage.DecodePixelHeight = 280;
                        photoImage.DecodePixelWidth = 180;
                        await photoImage.SetSourceAsync(photoStream);
                        img_left.Source = photoImage;
                    }
                    img_left.Visibility = Visibility.Visible;
                    LoadingControl.IsLoading = true;
                    await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                    await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(true);
                    //img_right.Source = new BitmapImage(new Uri(@"C:\go/result/output.jpg"));
                    StorageFile outputFile = await KnownFolders.PicturesLibrary.GetFileAsync("output.jpg");
                    using (IRandomAccessStream outputStream = await outputFile.OpenAsync(FileAccessMode.Read))
                    {
                        BitmapImage outputImage = new BitmapImage();
                        outputImage.DecodePixelHeight = 280;
                        outputImage.DecodePixelWidth = 180;
                        await outputImage.SetSourceAsync(outputStream);
                        img_right.Source = outputImage;
                    }
                    img_right.Visibility = Visibility.Visible;
                    LoadingControl.IsLoading = false;
                    break;
            }
        }

        async Task TakePicture()
        {
            var myPictures = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Pictures);
            StorageFile file = await myPictures.SaveFolder.CreateFileAsync("photo.jpg", CreationCollisionOption.ReplaceExisting);
            using (var captureStream = new InMemoryRandomAccessStream())
            {
                await mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), captureStream);

                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var decoder = await BitmapDecoder.CreateAsync(captureStream);
                    var encoder = await BitmapEncoder.CreateForTranscodingAsync(fileStream, decoder);

                    var properties = new BitmapPropertySet {
                        { "System.Photo.Orientation", new BitmapTypedValue(PhotoOrientation.Normal, PropertyType.UInt16) }
                    };
                    await encoder.BitmapProperties.SetPropertiesAsync(properties);

                    await encoder.FlushAsync();
                }
            }
        }

        private static async Task<BitmapImage> MyLoadImage(StorageFile file)
        {
            BitmapImage bitmapImage = new BitmapImage();
            FileRandomAccessStream stream = (FileRandomAccessStream)await file.OpenAsync(FileAccessMode.Read);
            bitmapImage.SetSource(stream);
            return bitmapImage;
        }

        private void combo_go_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int itemIdx = combo_go.SelectedIndex;
            switch (itemIdx)
            {
                case 0:
                    if (isShot == false)
                        img_right.Source = new BitmapImage(new Uri(this.BaseUri, "Assets/imgs/demo_background.jpg"));
                    else
                        if (isDemo)
                        img_right.Source = new BitmapImage(new Uri(this.BaseUri, "Assets/imgs/demo_output.jpg"));
                    //else
                    //DoNoghing
                    break;
                case 1:
                    MsgBox("오류", "파일이 없습니다.", "확인");
                    combo_go.SelectedIndex = 0;
                    break;
                case 2:
                    MsgBox("오류", "파일이 없습니다.", "확인");
                    combo_go.SelectedIndex = 0;
                    break;
                case 3:
                    MsgBox("오류", "파일이 없습니다.", "확인");
                    combo_go.SelectedIndex = 0;
                    break;
            }
        }
    }
}
