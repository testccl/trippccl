using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace tripp
{
    class DataManager
    {
        public DataManager()
        {
            InitializeFile();    
        }

        private async void InitializeFile()
        {
            IStorageItem tempFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync("LocationInfo.txt");

            if(tempFile == null)
            {
                await ApplicationData.Current.LocalFolder.CreateFileAsync("LocationInfo.txt");
            }
        }

        //GetLocationData- returns a locationData object based on the location string
        public async Task<LocationData> GetLocationData(string locationString)
        {
            List<LocationData> locations = await GetLocations();
            LocationData returnData = new LocationData();

            foreach(LocationData location in locations)
            {
                if (location.Location.Equals(locationString))
                    returnData = location;
            }

            return returnData;
        }

        public async Task<List<LocationData>> GetLocations()
        {

            //retrieve the data
            List<string> totalData = await GetFileContents();
            List<LocationData> returnList = new List<LocationData>();

            //loop through the data
            for(int index = 0; index < totalData.Count; index++)
            {
                LocationData returnElement = new LocationData();

                //for each location found
                if(totalData.ElementAt(index).Contains("<LOCATION:"))
                {
                    //extract relevant information
                    returnElement.Location = ExtractData(totalData.ElementAt(index));
                    while(!totalData.ElementAt(index).Contains("</LOCATION"))
                    {
                        if (totalData.ElementAt(index).Contains("<LATITUDE:"))
                            returnElement.Latitude = Convert.ToDouble(ExtractData(totalData.ElementAt(index)));
                        if (totalData.ElementAt(index).Contains("<LONGITUDE:"))
                            returnElement.Longitude = Convert.ToDouble(ExtractData(totalData.ElementAt(index)));
                        index++;
                    }

                    returnList.Add(returnElement);
                }
            }

            return returnList;
        }


        //AddLocationToDatabase- adds a location to the collection
        public async Task<int> AddLocationToDatabase(LocationData location)
        {
            //Check if this location is already in Database
            if (await AlreadyInDatabase(location))
                return 1;

            //retrieve the data
            List<string> totalData = await GetFileContents();

            //add the new data
            List<string> newData = new List<string>();

            newData.Add("<LOCATION:" + location.Location + ">");
            newData.Add("<LATITUDE:" + location.Latitude.ToString() + ">");
            newData.Add("<LONGITUDE:" + location.Longitude.ToString() + ">");
            newData.Add("</LOCATION>");

            await AppendDatabase(totalData, newData);
            
            return 0;

        }

        //RemoveFromDatabase- removes information from database
        public async Task<int> RemoveFromDatabase(LocationData location)
        {
            List<string> oldData = await GetFileContents();
            List<string> newData = new List<string>();
            for(int index = 0; index < oldData.Count; index++)
            {
                if(oldData.ElementAt(index).Equals("<LOCATION:" + location.Location + ">"))
                {
                    while(oldData.ElementAt(index).Equals("</LOCATION>"))
                    {
                        index++;
                    }

                    index++;
                }

                if (index < oldData.Count)
                    newData.Add(oldData.ElementAt(index));
            }

            //writeover the existing database;
            await AppendDatabase(newData, new List<string>());

            return 0;
        }

        //AppendDatabase- overwrites existing text file by replacing old and adding new
        public async Task<int> AppendDatabase(List<string> oldData, List<string> newData)
        {

            StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("LocationInfo.txt");
            
            Stream fileStream = await file.OpenStreamForWriteAsync();
            StreamWriter writer = new StreamWriter(fileStream);
            

            foreach(string entry in oldData)
            {
                writer.WriteLine(entry);
            }
            foreach (string entry in newData)
            {
                writer.WriteLine(entry);
            }

 
            await writer.FlushAsync();
            fileStream.Dispose();
            return 0;
        }


        //ExtractData
        private string ExtractData(string originalString)
        {
            string returnString = "";

            bool startCollection = false;
            //loop through string disregarding the last char (the >)
            for(int index = 0; index < originalString.Length - 1; index++)
            {
                if(startCollection)
                {
                    returnString = returnString + originalString[index];
                }
                if (originalString[index].Equals(':'))
                    startCollection = true;
            }

            return returnString;
        }
        //GetFileContents- returns the file contents.  Optional to filter by location
        public async Task<List<string>> GetFileContents(string location = null)
        {

            StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("LocationInfo.txt");
            Stream fileStream = await file.OpenStreamForReadAsync();
            StreamReader reader = new StreamReader(fileStream);

            List<string> returnList = new List<string>();

            //if location is given, filter for that

            if (location == null)
            {
                while (!reader.EndOfStream)
                {
                    returnList.Add(reader.ReadLine());
                }
            }
            else
            {
                while(!reader.EndOfStream)
                {
                    string tempString = reader.ReadLine();
                    if(tempString == "<LOCATION:" + location + ">")
                    {
                        returnList.Add(tempString);
                        while(returnList.ElementAt(returnList.Count- 1) != "</LOCATION:" + location + ">"
                                && !reader.EndOfStream)
                        {
                            returnList.Add(reader.ReadLine());
                        }
                    }
                }
            }

            fileStream.Dispose();
            return returnList;
        }

        private async Task<bool> AlreadyInDatabase(LocationData testLocation)
        {
            List<LocationData> refList = await GetLocations();

            foreach(LocationData refItem in refList)
            {
                if (Math.Round(testLocation.Latitude,5) == Math.Round(refItem.Latitude, 5)
                    && Math.Round(testLocation.Longitude, 5) == Math.Round(refItem.Longitude, 5)
                    && testLocation.Location.Equals(refItem.Location))
                    return true;
            }
            return false;
        }
    }

    public struct LocationData
    {
        public string Location{get; set;}
        public double Latitude{get; set;}
        public double Longitude{get; set;}

    }
}
