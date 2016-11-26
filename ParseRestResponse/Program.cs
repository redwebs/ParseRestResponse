using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using BingMapsRESTService.Common.JSON;
using System.Runtime.Serialization.Json;

// Original program from https://msdn.microsoft.com/en-us/library/hh674188.aspx

namespace ParseRestResponse
{
    class Program
    {
        private static string _bingMapsKey = string.Empty;
        // Create Web Api client just once to avoid running out of ports
        private static readonly HttpClient Client = new HttpClient();
        private const string BingRestLocation = "http://dev.virtualearth.net/REST/v1/";


        static void Main(string[] args)
        {
            try
            {
                _bingMapsKey = Environment.GetEnvironmentVariable("BingMapsKey");

                // First search for a general location.  We could also put a street address here also.
                const string locationRequestStr = "New York";

                var place = WebUtility.UrlEncode(locationRequestStr);

                var urlRequest = $"{BingRestLocation}Locations/{place}?key={_bingMapsKey}";

                var locationsResponse = MakeRequestWebApi(urlRequest);

                if (locationsResponse.Item2 != "success")
                {
                    Console.WriteLine($"Bing Maps Request fail, message: {locationsResponse.Item2}");
                }
                else
                {
                    ProcessLocationResponse(locationsResponse.Item1);
                }

                // Now find a route, first geocode two locations

                // First location

                const string brillBuildingAddress = "1619 Broadway, New York, NY 10019";

                place = WebUtility.UrlEncode(brillBuildingAddress);

                urlRequest = $"{BingRestLocation}Locations/{place}?key={_bingMapsKey}";

                var fromLocationResponse = MakeRequestWebApi(urlRequest);

                if (fromLocationResponse.Item2 != "success")
                {
                    Console.WriteLine($"Bing Maps Location Request fail, message: {fromLocationResponse.Item2}");
                    Console.WriteLine("Press any key to exit");
                    Console.ReadKey();
                    return;
                }
                var brillResouresSet = fromLocationResponse.Item1.ResourceSets[0];
                var brillLocation = (Location) brillResouresSet.Resources[0];
                // Second location

                const string staxStudiosAddress = "926 E McLemore Ave, Memphis, TN 38126";
                place = WebUtility.UrlEncode(staxStudiosAddress);

                urlRequest = $"{BingRestLocation}Locations/{place}?key={_bingMapsKey}";

                var toLocationResponse = MakeRequestWebApi(urlRequest);

                if (toLocationResponse.Item2 != "success")
                {
                    Console.WriteLine($"Bing Maps Location Request fail, message: {toLocationResponse.Item2}");
                    Console.WriteLine("Press any key to exit");
                    Console.ReadKey();
                    return;
                }
                var staxResouresSet = fromLocationResponse.Item1.ResourceSets[0];
                var staxLocation = (Location) staxResouresSet.Resources[0];


                // Now call the Routes API
                var routeUrl = $"{BingRestLocation}Routes?wp.0={brillLocation.Point.Coordinates[0]},{brillLocation.Point.Coordinates[1]}" +
                               $"&wp.1={staxLocation.Point.Coordinates[0]},{staxLocation.Point.Coordinates[1]}&du=mile&key={_bingMapsKey}";

                var routeResponse = MakeRequestWebApi(routeUrl);

                if (routeResponse.Item2 != "success")
                {
                    Console.WriteLine($"Bing Maps Route Request fail, message: {routeResponse.Item2}");
                    Console.WriteLine("Press any key to exit");
                    Console.ReadKey();
                    return;
                }

 
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        /// <summary>
        /// Makes Web API call to the request URL and returns a Bing Response Object 
        /// </summary>
        /// <param name="requestUrl"></param>
        /// <returns>Bing Response Object which must be cast to one of these objects:
        ///      Location, Route, Traffic Incident, CompressedPointList, ElevationData, SeaLevelData
        /// </returns>
        public static Tuple<Response, string> MakeRequestWebApi(string requestUrl)
        {
            var httpResponseMessage = Client.GetAsync(requestUrl).Result;

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                return new Tuple<Response, string>(null, $"Response Status: {httpResponseMessage.StatusCode}");
            }

            var jsonString = httpResponseMessage.Content.ReadAsStringAsync().Result;

            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
            {
                var deserializer = new DataContractJsonSerializer(typeof(Response));
                return new Tuple<Response, string>((Response)deserializer.ReadObject(ms), "success");
            }
        }

        public static void ProcessLocationResponse(Response locationsResponse)
        {
            var locsCount = locationsResponse.ResourceSets[0].Resources.Length;

            // Get all locations in the response and then extract the formatted address for each location
            Console.WriteLine("Show all formatted addresses");

            var resouresSet = locationsResponse.ResourceSets[0];

            for (var loc = 0; loc < locsCount; loc++)
            {
                var locationResource = (Location) resouresSet.Resources[loc];
                var addr = locationResource.Address.FormattedAddress;
                Console.WriteLine(addr);
            }
            Console.WriteLine();

            // Get the Geocode Points for each Location
            for (var loc = 0; loc < locsCount; loc++)
            {
                var locationResource = (Location) resouresSet.Resources[loc];
                Console.WriteLine("Geocode Points for " + locationResource.Address.FormattedAddress);

                var countGeocodePoints = locationResource.GeocodePoints.Length;

                for (var point = 0; point < countGeocodePoints; point++)
                {
                    var pointInfo = locationResource.GeocodePoints[point];

                    Console.WriteLine($"    Point: {pointInfo.Coordinates[0]}, {pointInfo.Coordinates[1]}");

                    Console.Write("    Usages: ");
                    for (var usage = 0; usage < pointInfo.UsageTypes.Length; usage++)
                    {
                        Console.Write($"{pointInfo.UsageTypes[usage]} ");
                    }
                    Console.WriteLine("\n\n");
                }
            }
            Console.WriteLine();


            // Get all locations that have a Confidence=High
            Console.WriteLine("Locations that have Confidence=High");

            for (var loc = 0; loc < locsCount; loc++)
            {
                var locationResource = (Location)resouresSet.Resources[loc];

                if (locationResource.Confidence == "High")
                {
                    Console.WriteLine(locationResource.Address.FormattedAddress);
                }
            }
            Console.WriteLine();
        }
        public static void ProcessGeoCode(Response routesResponse)
        {
            // Get first location in the response and then extract the lat and long
            Console.WriteLine("Getting Lat and Long");

            var resouresSet = routesResponse.ResourceSets[0];
            var location = (Location) resouresSet.Resources[0];

            Console.WriteLine();

            // Get the Geocode Points for each Location
            for (var loc = 0; loc < locsCount; loc++)
            {
                var locationResource = (Location) resouresSet.Resources[loc];
                Console.WriteLine("Geocode Points for " + locationResource.Address.FormattedAddress);

                var countGeocodePoints = locationResource.GeocodePoints.Length;

                for (var point = 0; point < countGeocodePoints; point++)
                {
                    var pointInfo = locationResource.GeocodePoints[point];

                    Console.WriteLine($"    Point: {pointInfo.Coordinates[0]}, {pointInfo.Coordinates[1]}");

                    Console.Write("    Usages: ");
                    for (var usage = 0; usage < pointInfo.UsageTypes.Length; usage++)
                    {
                        Console.Write($"{pointInfo.UsageTypes[usage]} ");
                    }
                    Console.WriteLine("\n\n");
                }
            }
            Console.WriteLine();


            // Get all locations that have a Confidence=High
            Console.WriteLine("Locations that have Confidence=High");

            for (var loc = 0; loc < locsCount; loc++)
            {
                var locationResource = (Location)resouresSet.Resources[loc];

                if (locationResource.Confidence == "High")
                {
                    Console.WriteLine(locationResource.Address.FormattedAddress);
                }
            }
            Console.WriteLine();
        }

        public static void ProcessRoutesResponse(Response routesResponse)
        {
            
        }
    }
}
