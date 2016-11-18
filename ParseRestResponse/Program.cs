using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BingMapsRESTService.Common.JSON;
using Newtonsoft.Json;
using System.Runtime.Serialization.Json;

// Original from https://msdn.microsoft.com/en-us/library/hh674188.aspx

namespace ParseRestResponse
{
    class Program
    {
        private static string _bingMapsKey = string.Empty;
        private static readonly HttpClient Client = new HttpClient();
        //private static readonly string BingMapsKey = ApplicationSettingsManager.GetBingMapKey();
        private const string UrlLocation = "http://dev.virtualearth.net/REST/v1/Locations?query=";


        static void Main(string[] args)
        {
            try
            {
                _bingMapsKey = Environment.GetEnvironmentVariable("BingMapsKey");
                string locationsRequest = CreateRequest("New%20York");
//                Response locationsResponse = MakeRequest(locationsRequest);
                Response locationsResponse = MakeRequestWebApi(locationsRequest);
                ProcessResponse(locationsResponse);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.Read();
            }

        }

        //Create the request URL
        public static string CreateRequest(string queryString)
        {
            string urlRequest = "http://dev.virtualearth.net/REST/v1/Locations/" +
                                           queryString +
                                           "?key=" + _bingMapsKey;
            return (urlRequest);
        }

        
        List<Address> _foundAddresses = new List<Address>();

        private static T DeserializeJson<T>(string json)
        {
            var instance = typeof(T);
            var lst = new List<Address>();

            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
            {
                DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(T));
                return (T)deserializer.ReadObject(ms);
            }
        }
        /*
            someJsonArrayString = "[{\"ID\":7},{\"ID\":16}]";
            filesToMove = deserializeJSON<List<SomeDataClass>>(someJsonArrayString);
            Console.WriteLine(filesToMove[1].ID); // returns 16
             
        */

        public static Response MakeRequestWebApi(string requestUrl)
        {
            //            var url = $"{UrlLocation}{address}&key={BingMapsKey}";

            HttpResponseMessage response = Client.GetAsync(requestUrl).Result;

            if (response.IsSuccessStatusCode)
            {
                var jsonString = response.Content.ReadAsStringAsync().Result;
                //var bingResponse = JsonConvert.DeserializeObject<Response>(jsonString);
                //return bingResponse;

                //byte[] bytes = Encoding.UTF8.GetBytes(response.Content.ToString());
                //var locsStream = new MemoryStream(bytes);
                //locsStream.Position = 0;


                //var jasonSerializer = new DataContractJsonSerializer(typeof(Response));
                //var locsResponse = (Response)jasonSerializer.ReadObject(locsStream);

                var mapResponse = DeserializeJson<Response>(jsonString);



                return mapResponse;

            }

            return null;
        }

        public static Response MakeRequest(string requestUrl)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(requestUrl) as HttpWebRequest;
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new Exception(String.Format(
                        "Server error (HTTP {0}: {1}).",
                        response.StatusCode,
                        response.StatusDescription));
                    DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(Response));
                    object objResponse = jsonSerializer.ReadObject(response.GetResponseStream());
                    Response jsonResponse
                    = objResponse as Response;
                    return jsonResponse;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

        }

        static public void ProcessResponse(Response locationsResponse)
        {

            int locNum = locationsResponse.ResourceSets[0].Resources.Length;

            //Get formatted addresses: Option 1
            //Get all locations in the response and then extract the formatted address for each location
            Console.WriteLine("Show all formatted addresses");
            for (int i = 0; i < locNum; i++)
            {

//                Location location = locationsResponse.ResourceSets[0].Resources[i];
//                Console.WriteLine(location.Address.FormattedAddress);

                var resouresSets = (ResourceSet) locationsResponse.ResourceSets[0];
                var resource = (Location) resouresSets.Resources[i];
                var addr = resource.Address.FormattedAddress;
                var x = 2;
                Console.WriteLine(addr);
            }
            Console.WriteLine();


            //Get the Geocode Points for each Location
            for (int i = 0; i < locNum; i++)
            {
                Location location = (Location)locationsResponse.ResourceSets[0].Resources[i];
                Console.WriteLine("Geocode Points for " + location.Address.FormattedAddress);
                int geocodePointNum = location.GeocodePoints.Length;
                for (int j = 0; j < geocodePointNum; j++)
                {
                    Console.WriteLine("    Point: " + location.GeocodePoints[j].Coordinates[0].ToString() + "," +
                                                 location.GeocodePoints[j].Coordinates[1].ToString());
                    double test = location.GeocodePoints[j].Coordinates[1];
                    Console.Write("    Usage: ");
                    for (int k = 0; k < location.GeocodePoints[j].UsageTypes.Length; k++)
                    {
                        Console.Write(location.GeocodePoints[j].UsageTypes[k].ToString() + " ");
                    }
                    Console.WriteLine("\n\n");
                }
            }
            Console.WriteLine();


            //Get all locations that have a MatchCode=Good and Confidence=High
            Console.WriteLine("Locations that have a Confidence=High");
            for (int i = 0; i < locNum; i++)
            {
                Location location = (Location)locationsResponse.ResourceSets[0].Resources[i];
                if (location.Confidence == "High")
                    Console.WriteLine(location.Address.FormattedAddress);
            }
            Console.WriteLine();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();


        }
    }
}
