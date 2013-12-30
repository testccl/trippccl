using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Bing.Maps;
using Windows.UI;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace tripp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Bing.Maps.Search.LocationDataResponse currentSearchResults;
        

        public MainPage()
        {
            this.InitializeComponent();
            HideSidePanels();
            AddPushpins();
        }

        public async Task<Bing.Maps.Search.LocationDataResponse> PerformSearch(string searchString)
        {
            // Set the address string to geocode
            Bing.Maps.Search.GeocodeRequestOptions requestOptions = new Bing.Maps.Search.GeocodeRequestOptions(searchString);

            // Make the geocode request 
            Bing.Maps.Search.SearchManager searchManager = MapMain.SearchManager;
            Bing.Maps.Search.LocationDataResponse response = await searchManager.GeocodeAsync(requestOptions);

            return response;
        }

        public void HideSidePanels()
        {
            ColumnDefinitionCollection cols = GridBody.ColumnDefinitions;
            ColumnDefinition column2 = cols.ElementAt(2);
            ColumnDefinition column0 = cols.ElementAt(0);
            column2.Width = new GridLength(column2.MinWidth);
            column0.Width = new GridLength(column0.MinWidth);
        }

        //ShowSidePanel- shows a particular side panel
        private void ShowSidePanel(SidePanels panel)
        {
            int panelNumber = 0;

            if (panel == SidePanels.SearchPanel)
                panelNumber = 2;


            ColumnDefinitionCollection cols = GridBody.ColumnDefinitions;
            ColumnDefinition column = cols.ElementAt(panelNumber);
            column.Width = new GridLength(column.MaxWidth);

        }

        //enum for side panel determination
        private enum SidePanels
        {
            SearchPanel,
            HelpPanel
        }

        //TappedAddLocation- event handler for the add location button
        private void TappedAddLocation(object sender, TappedRoutedEventArgs e)
        {
            //If add location is out, hide it and vice-versa
            ColumnDefinitionCollection cols = GridBody.ColumnDefinitions;
            ColumnDefinition column = cols.ElementAt(2);

            //hide or display the menu
            if (column.Width == new GridLength(column.MinWidth))
                ShowSidePanel(SidePanels.SearchPanel);
            else
                HideSidePanels();
        }

        //ButtonClickSearchLocations- event handler that searches locations based on user input
        private async void ButtonClickSearchLocations(object sender, RoutedEventArgs e)
        {
            //Clear the current results
            StackPanelLocationResults.Children.Clear();
            currentSearchResults = null;

            //retrieve the location data and store it in the global variable
            Task<Bing.Maps.Search.LocationDataResponse> getLocDataTask = PerformSearch(TextBoxSearch.Text);
            Bing.Maps.Search.LocationDataResponse returnedData = await getLocDataTask;
            currentSearchResults = returnedData;

            //Add the results to the stackpanel
            for(int index = 0; index < returnedData.LocationData.Count; index++)
            {
                TextBlock locationTextBlock = CreateSearchResultTextBlock(returnedData.LocationData[index].Name);
                StackPanelLocationResults.Children.Add(locationTextBlock);
            }

        }

        //CreateSearchResultTextBlock- creates the text blocks of the search results than go into the stack panel
        private TextBlock CreateSearchResultTextBlock(string location)
        {
            TextBlock returnTextBlock = new TextBlock();
            returnTextBlock.Text = location;
            returnTextBlock.Width = GridSearch.Width;
            returnTextBlock.Height = 100;
            returnTextBlock.FontSize = 20;
            returnTextBlock.TextAlignment = TextAlignment.Center;
            returnTextBlock.Tapped += SearchResultTapped;

            return returnTextBlock;

        }


        //SearchResultTapped- one of the search results was selected
        private async void SearchResultTapped(object sender, TappedRoutedEventArgs e)
        {
            //cast the sender object correctly
            TextBlock senderElement = (TextBlock)sender;

            //loop through all of the currentSearchResults to find the selected one
            for(int index = 0; index < currentSearchResults.LocationData.Count; index++)
            {
                //if the match is found
                if(senderElement.Text.Equals(currentSearchResults.LocationData.ElementAt(index).Name))
                {
                    

                    //add this to the database
                    LocationData newData = new LocationData();
                    newData.Location = currentSearchResults.LocationData.ElementAt(index).Name;
                    newData.Longitude = currentSearchResults.LocationData.ElementAt(index).Location.Longitude;
                    newData.Latitude = currentSearchResults.LocationData.ElementAt(index).Location.Latitude;

                    DataManager manager = new DataManager();
                    await manager.AddLocationToDatabase(newData);
                    AddPushpins();
                }

            }
        }

        public async void AddPushpins()
        {
            MapMain.Children.Clear();
            MapMain.Children.Add(new MapLayer());
            DataManager manager = new DataManager();
            MapLayer pushPinLayer = (MapLayer)MapMain.Children.ElementAt(0);

            List<LocationData> locations = await manager.GetLocations();

            foreach(LocationData location in locations)
            {
                Pushpin pushpin = new Pushpin();
                pushpin.Name = location.Location;
                pushpin.Background = new SolidColorBrush(Colors.Blue);

                //add the event handlers
                pushpin.PointerEntered += DisplayLocation;
                pushpin.PointerExited += DisplayTripp;
                pushpin.Tapped += PushpinTapped;

                //set the position
                MapLayer.SetPosition(pushpin,
                            new Location(location.Latitude,location.Longitude));

                //add to parent
                pushPinLayer.Children.Add(pushpin);
            }
            

        }

        //PushpinTapped- handles the event of selecting a pin
        private void PushpinTapped(object sender, TappedRoutedEventArgs e)
        {
            Pushpin senderPin = (Pushpin) sender;

            //cast the sender object appropriately
            this.Frame.Navigate(typeof(tripp.LocationPage), senderPin.Name);
        }

        private void DisplayTripp(object sender, PointerRoutedEventArgs e)
        {
            Pushpin senderPin = (Pushpin)sender;
            TextBlockTripp.Text = "tripp";
            senderPin.Background = new SolidColorBrush(Colors.Blue);
        }

        private void DisplayLocation(object sender, PointerRoutedEventArgs e)
        {
            Pushpin senderPin = (Pushpin)sender;
            TextBlockTripp.Text = senderPin.Name;
            senderPin.Background = new SolidColorBrush(Colors.Red);

        }

        
    }


    

}
