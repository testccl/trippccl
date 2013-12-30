using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Storage.FileProperties;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace tripp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LocationPage : Page
    {
        string location = "";
        bool deleteImageMode = false;
        public LocationPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            location = e.Parameter as string;
            TextBlockLocation.Text = e.Parameter as string;
            InitializePictures();
            HideSettings();
        }

        //HideSettings- hides the settings options
        private void HideSettings()
        {
            foreach(object child in StackPanelContent.Children)
            {
                Grid childGrid = (Grid) child;
                if(childGrid.Name.Contains("Settings"))
                {
                    childGrid.Height = childGrid.MinHeight;
                }
            }
        }
        private async void ButtonClickPickPicture(object sender, RoutedEventArgs e)
        {
            // Set up and launch the Open Picker
            FileOpenPicker fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.ViewMode = PickerViewMode.Thumbnail;
            fileOpenPicker.FileTypeFilter.Add(".png");
            fileOpenPicker.FileTypeFilter.Add(".jpg");
            fileOpenPicker.FileTypeFilter.Add(".jpeg");

            //retrieve the files to be stored
            IReadOnlyList<StorageFile> files = await fileOpenPicker.PickMultipleFilesAsync();
            StringBuilder fileNames = new StringBuilder();

            //make sure the proper subfolder exists, if not, create it
            bool doesCurrentSubfolderExist = await DoesFolderExist();
            if(!doesCurrentSubfolderExist)
            {
                await CreateSubfolder();
            }

            //retrieve the appropriate subfolder

            StorageFolder pictureFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(location);

            foreach (StorageFile file in files)
            {
                // At this point, the app can begin reading from the provided file
                fileNames.AppendLine(file.Path);
                IStorageItem compItem = await pictureFolder.TryGetItemAsync(file.Name);
                if (compItem == null)
                {
                    await file.CopyAsync(pictureFolder, file.Name);
                }
            }
            
            //refresh the picture output
            InitializePictures();
        }

        private async Task<bool> DoesFolderExist()
        {
            IReadOnlyList<StorageFolder> folders = await ApplicationData.Current.LocalFolder.GetFoldersAsync();

            foreach(StorageFolder folder in folders)
            {
                if (folder.Name.Equals(location))
                    return true;
            }

            return false;
        }

        private async void  InitializePictures()
        {

            //Clear the current pictures
            StackPanelPictures.Children.Clear();
            //Update pictures

            //make sure folder exists
            IStorageItem testItem = await ApplicationData.Current.LocalFolder.TryGetItemAsync(location);
            if (testItem == null)
                return;

            //get the picture folder and then the files it contains
            StorageFolder pictureFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(location);
            IReadOnlyList<StorageFile> pictureFiles = await pictureFolder.GetFilesAsync();

            //get the height of the pictures
            int height = (int)GridPhotos.RowDefinitions[0].Height.Value;

            //Load each picture into the stackpanel
            foreach(StorageFile pictureFile in pictureFiles)
            {
                WriteableBitmap pictureBm = new WriteableBitmap(190, 130);
                
                IRandomAccessStream fileStream = await pictureFile.OpenAsync(Windows.Storage.FileAccessMode.Read);
                StorageItemThumbnail thumbnail = await pictureFile.GetThumbnailAsync(ThumbnailMode.PicturesView);
                pictureBm.SetSource(thumbnail);
                Image pagePicture = new Image();
                pagePicture.Margin = new Windows.UI.Xaml.Thickness(5);

                pagePicture.Source = pictureBm;
                pagePicture.Height = StackPanelPictures.Height;
                pagePicture.Width = StackPanelPictures.Width;

                pagePicture.Tapped += ImageTapped;
                pagePicture.Name = pictureFile.Name;
                StackPanelPictures.Children.Add(pagePicture);

            }
            
        }

        private async void ImageTapped(object sender, TappedRoutedEventArgs e)
        {
            Image senderImage = (Image)sender;
            StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(location);
            StorageFile file = await folder.GetFileAsync(senderImage.Name);

            if (!deleteImageMode)
            {
                await Windows.System.Launcher.LaunchFileAsync(file);
            }
            else
            {
                await file.DeleteAsync();
                InitializePictures();
            }
            
            
        }


        private async Task<int> CreateSubfolder()
        {
            await ApplicationData.Current.LocalFolder.CreateFolderAsync(location);

            return 0;
        }

        private void ButtonClickDeletePicture(object sender, RoutedEventArgs e)
        {
            if(deleteImageMode)
            {
                ButtonPickPicture.IsEnabled = true;
            }
            else
            {
                ButtonPickPicture.IsEnabled = false;
            }

            deleteImageMode = !deleteImageMode;
        }

        private async void ButtonClickDeleteLocation(object sender, RoutedEventArgs e)
        {
            //Delete entry from the database
            DataManager manager = new DataManager();
            await manager.RemoveFromDatabase(await manager.GetLocationData(location));

            //Delete pictures from the application data if picture folder exists

            try
            {
                StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(location);
                await folder.DeleteAsync();
            }
            catch(System.IO.FileNotFoundException)
            {

            }

            this.Frame.Navigate(typeof(tripp.MainPage));
            
        }

        private void TappedBack(object sender, TappedRoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(tripp.MainPage));
        }

        private void TappedEdit(object sender, TappedRoutedEventArgs e)
        {
            foreach (object child in StackPanelContent.Children)
            {
                Grid childGrid = (Grid)child;
                if (childGrid.Name.Contains("Settings"))
                {
                    if (childGrid.Height == childGrid.MaxHeight)
                        childGrid.Height = childGrid.MinHeight;
                    else
                        childGrid.Height = childGrid.MaxHeight;
                }
            }
        }
    }
}
