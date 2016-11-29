using System;
using System.Collections.Generic;
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

                // Bing API Structured URLs: Get the latitude and longitude coordinates based on a set of address values for specific countries
                // US format:
                // http://dev.virtualearth.net/REST/v1/Locations/US/adminDistrict/postalCode/locality/addressLine?includeNeighborhood=includeNeighborhood&include=includeValue&maxResults=maxResults&key=BingMapsKey
                // UK format:
                // http://dev.virtualearth.net/REST/v1/Locations/UK/postalCode?includeNeighborhood=includeNeighborhood&include=includeValue&maxResults=maxResults&key=BingMapsKey

                var urlRequest = $"{BingRestLocation}Locations/{place}?key={_bingMapsKey}";

                var locationsResponse = MakeRequestWebApi(urlRequest);

                if (locationsResponse.Item2 != "success")
                {
                    Console.WriteLine($"Bing Maps Request fail, message: {locationsResponse.Item2}");
                    Console.WriteLine("Press any key to exit");
                }
                else
                {
                    ProcessLocationResponse(locationsResponse.Item1, locationRequestStr);
                }

                // Now find a route, first geocode two locations

                // First location

                // Unstructured URL: Get the latitude and longitude coordinates based on a set of address values for any country
                //   In the format:
                // http://dev.virtualearth.net/REST/v1/Locations?countryRegion=countryRegion&adminDistrict=adminDistrict&locality=locality&postalCode=postalCode&addressLine=addressLine&userLocation=userLocation&userIp=userIp&usermapView=usermapView&includeNeighborhood=includeNeighborhood&maxResults=maxResults&key=BingMapsKey

                const string brillBuildingAddress = "1619 Broadway New York NY 10019";

                place = Uri.EscapeDataString(brillBuildingAddress);

                urlRequest = $"{BingRestLocation}Locations?query={place}&key={_bingMapsKey}";

                var fromLocationResponse = MakeRequestWebApi(urlRequest);

                if (fromLocationResponse.Item2 != "success")
                {
                    Console.ReadKey();
                    return;
                }
                ProcessLocationResponse(fromLocationResponse.Item1, brillBuildingAddress);

                var brillResouresSet = fromLocationResponse.Item1.ResourceSets[0];
                var brillLocation = (Location) brillResouresSet.Resources[0];

                // Second location

                const string staxStudiosAddress = "926 E McLemore Ave, Memphis, TN 38126";
                place = Uri.EscapeDataString(staxStudiosAddress);

                urlRequest = $"{BingRestLocation}Locations?query={place}&key={_bingMapsKey}";

                var toLocationResponse = MakeRequestWebApi(urlRequest);

                if (toLocationResponse.Item2 != "success")
                {
                    Console.ReadKey();
                    return;
                }
                ProcessLocationResponse(toLocationResponse.Item1, staxStudiosAddress);
                var staxResouresSet = toLocationResponse.Item1.ResourceSets[0];
                var staxLocation = (Location) staxResouresSet.Resources[0];

                // Third location

                const string whiskyaGoGoAddress = "8901 W. Sunset Blvd West Hollywood, CA 90069";
                place = Uri.EscapeDataString(whiskyaGoGoAddress);

                urlRequest = $"{BingRestLocation}Locations?query={place}&key={_bingMapsKey}";

                var endLocationResponse = MakeRequestWebApi(urlRequest);

                if (endLocationResponse.Item2 != "success")
                {
                    Console.ReadKey();
                    return;
                }
                ProcessLocationResponse(endLocationResponse.Item1, staxStudiosAddress);
                var whiskyResouresSet = endLocationResponse.Item1.ResourceSets[0];
                var whiskyLocation = (Location)whiskyResouresSet.Resources[0];

                // Now call the Routes API

                var wayPoints = new List<Location> {brillLocation, staxLocation, whiskyLocation};

                var routeUrl =
                    $"{BingRestLocation}Routes?{MakeWaypointString(wayPoints)}du=mile&key={_bingMapsKey}";

                var routeResponse = MakeRequestWebApi(routeUrl);

                if (routeResponse.Item2 != "success")
                {
                    Console.WriteLine($"Bing Maps Route Request fail, message: {routeResponse.Item2}");
                    Console.WriteLine("Press any key to exit");
                    Console.ReadKey();
                    return;
                }
                ProcessRouteResponse(routeResponse.Item1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        public static string MakeWaypointString(List<Location> waypoints)
        {
            var waypointsSb = new StringBuilder();
            var waypointCntr = 1;

            foreach (var waypoint in waypoints)
            {
                waypointsSb.Append($"wp.{waypointCntr}=");
                waypointsSb.Append($"{waypoint.Point.Coordinates[0]},");
                waypointsSb.Append($"{waypoint.Point.Coordinates[1]}&");
                waypointCntr++;
            }
            return waypointsSb.ToString();
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
                var deserializer = new DataContractJsonSerializer(typeof (Response));
                return new Tuple<Response, string>((Response) deserializer.ReadObject(ms), "success");
            }
        }

        public static void ProcessLocationResponse(Response locationsResponse, string placeName)
        {
            var locsCount = locationsResponse.ResourceSets[0].Resources.Length;

            if (locsCount == 0)
            {
                Console.WriteLine($"No resources returned for {placeName}");
                return;
            }

            // Get all locations in the response and then extract the formatted address for each location
            Console.WriteLine($"Show all formatted addresses for {placeName}");

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
                var locationResource = (Location) resouresSet.Resources[loc];

                if (locationResource.Confidence == "High")
                {
                    Console.WriteLine(locationResource.Address.FormattedAddress);
                }
            }
            Console.WriteLine();
        }
        public static void ProcessRouteResponse(Response routeResponse)
        {
            if (routeResponse.ResourceSets.Length == 0)
            {
                Console.WriteLine("No Resource Set returned for route request");
                return;
            }

            var routesCount = routeResponse.ResourceSets[0].Resources.Length;

            if (routesCount == 0)
            {
                Console.WriteLine("No Resources returned in Resource Set for route request");
                return;
            }

            var routeResource = (Route)routeResponse.ResourceSets[0].Resources[0];

            var legCount = routeResource.RouteLegs.Length;
            Console.WriteLine($"Route has {legCount} legs");

            for (var leg = 0; leg < legCount; leg++)
            {
                var itenCount = routeResource.RouteLegs[leg].ItineraryItems.Length;

                // Warning! A StartLocation is provided only when the waypoint is specified as a landmark or an address. 
                //     routeResource.RouteLegs[leg].StartLocation.GeocodePoints[0].Coordinates[0]
                // So use: routeResource.RouteLegs[leg].ActualStart.Coordinates[0]

                Console.WriteLine($"Leg-{leg + 1} Itenerary Count: {itenCount}, " +
                                  $"Start Lat:{routeResource.RouteLegs[leg].ActualStart.Coordinates[0]}, " +
                                  $"Start Long:{routeResource.RouteLegs[leg].ActualStart.Coordinates[1]}");

                for (var iten = 0; iten < itenCount; iten++)
                {
                    Console.WriteLine($"Itenerary-{iten} - {routeResource.RouteLegs[leg].ItineraryItems[iten].Instruction.Text}");
                    Console.WriteLine($"   Coordinates: {routeResource.RouteLegs[leg].ItineraryItems[iten].ManeuverPoint.Coordinates[0]}" +
                                      $"{routeResource.RouteLegs[leg].ItineraryItems[iten].ManeuverPoint.Coordinates[1]}");
                }
            }
            Console.WriteLine($"Total Distance = {routeResource.TravelDistance} {routeResource.DistanceUnit}");

            var totalSecsInt = (int)routeResource.TravelDuration;
            
            var hours = totalSecsInt / 3600;
            var mins = (totalSecsInt - (hours * 3600)) / 60;
            var secs = totalSecsInt - (hours * 3600) - (mins * 60);

            Console.WriteLine($"Total Duration Total Seconds = {routeResource.TravelDuration} {routeResource.DurationUnit}");
            Console.WriteLine($"Total Duration Time = {hours}:{mins}:{secs}");

        }

    }
}
