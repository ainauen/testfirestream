using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
//using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Rest;
using Microsoft.Identity.Client;

using System.Text.Json;

namespace FireStream.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StreamingLocatorController : ControllerBase
    {
        //private static readonly string[] Summaries = new[]
        //{
        //    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        //};

        private static List<string> BuildManifestPaths(string scheme, string hostname, string streamingLocatorId, string manifestName)
        {
            const string hlsFormat = "format=m3u8-cmaf";
            const string dashFormat = "format=mpd-time-cmaf";

            List<string> manifests = new();

            var manifestBase = $"{scheme}://{hostname}/{streamingLocatorId}/{manifestName}.ism/manifest";
            var hlsManifest = $"{manifestBase}({hlsFormat})";
            manifests.Add(hlsManifest);

            //var dashManifest = $"{manifestBase}({dashFormat})";
            var dashManifest = $"{manifestBase}";
            manifests.Add(dashManifest);

            return manifests;
        }

        private static async Task<StreamingLocator> CreateStreamingLocatorAsync(
            IAzureMediaServicesClient client,
            string resourceGroup,
            string accountName,
            string assetName,
            string locatorName)
        {
            Console.WriteLine("Creating a streaming locator...");
            StreamingLocator locator = await client.StreamingLocators.CreateAsync(
                resourceGroup,
                accountName,
                locatorName,
                new StreamingLocator
                {
                    AssetName = assetName,
                    StreamingPolicyName = PredefinedStreamingPolicy.ClearStreamingOnly
                });

            return locator;
        }




        private readonly ILogger<StreamingLocatorController> _logger;

        public StreamingLocatorController(ILogger<StreamingLocatorController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<IEnumerable<MyStreamingLocator>> GetAsync()
        {
            List<string> urls = new List<string>();
            System.Diagnostics.Debug.WriteLine("test at1");
            // Your Azure Media Service (AMS) Account Name
            string ACCOUNT_NAME = "test0fire0account";
            // Your Resource Group Name
            string RESOURCE_GROUP_NAME = "test_fire_stream";

            string LIVE_EVENT = "test0live0event";


            string uniqueness = Guid.NewGuid().ToString().Substring(0, 13); // Create a GUID for uniqueness. You can make this something static if you dont want to change RTMP ingest settings in OBS constantly.  
            string liveEventName = "liveevent-" + uniqueness; // WARNING: Be careful not to leak live events using this sample!
            string assetName = "testarchiveAsset" + uniqueness;
            string liveOutputName = "liveOutput" + uniqueness;
            //string drvStreamingLocatorName = "streamingLocator" + uniqueness;
            string drvStreamingLocatorName = "streamingLocator" + uniqueness;
            string archiveStreamingLocatorName = "fullLocator-" + uniqueness;
            string drvAssetFilterName = "filter-" + uniqueness;
            string streamingLocatorName = "streamingLocator" + uniqueness;
            string streamingEndpointName = "default";
            //string manifestName = "output";

            System.Diagnostics.Debug.WriteLine("test at");

            //return Enumerable(new StreamingLocator { Date = DateTime.Now.AddDays(1), Summary = "one item" }).toArray();
            Console.WriteLine("test");
            IAzureMediaServicesClient client = await Authentication.CreateMediaServicesClientAsync();
            Console.WriteLine(client.Assets);
            try
            {
                // get media service
                MediaService mediaService = await client.Mediaservices.GetAsync(RESOURCE_GROUP_NAME, ACCOUNT_NAME);

                string jsonString = JsonSerializer.Serialize(mediaService);
                
                System.Diagnostics.Debug.WriteLine(jsonString);
                //get live event
                LiveEvent liveEvent = await client.LiveEvents.GetAsync(RESOURCE_GROUP_NAME, ACCOUNT_NAME, LIVE_EVENT);



                Asset asset = await client.Assets.CreateOrUpdateAsync(RESOURCE_GROUP_NAME, ACCOUNT_NAME, assetName, new Asset());

                int testint = client.LiveOutputs.List(RESOURCE_GROUP_NAME, ACCOUNT_NAME, LIVE_EVENT).Count();
                System.Diagnostics.Debug.WriteLine($"Live outputs{testint}");

                if (testint == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Creating an asset named {assetName}");
                    string manifestName = "output";
                    System.Diagnostics.Debug.WriteLine($"Creating a live output named {liveOutputName}");
                    LiveOutput liveOutput = new(
                        assetName: assetName,
                        manifestName: manifestName, // The HLS and DASH manifest file name. This is recommended to set if you want a deterministic manifest path up front.
                                                    // archive window can be set from 3 minutes to 25 hours. Content that falls outside of ArchiveWindowLength
                                                    // is continuously discarded from storage and is non-recoverable. For a full event archive, set to the maximum, 25 hours.
                        archiveWindowLength: TimeSpan.FromHours(1)
                        );



                    liveOutput = await client.LiveOutputs.CreateAsync(
                        RESOURCE_GROUP_NAME,
                        ACCOUNT_NAME,
                        LIVE_EVENT,
                        liveOutputName,
                        liveOutput);

                    System.Diagnostics.Debug.WriteLine("liveOutput made");
                    int testint2 = client.LiveOutputs.List(RESOURCE_GROUP_NAME, ACCOUNT_NAME, LIVE_EVENT).Count();
                    System.Diagnostics.Debug.WriteLine($"Creating an asset named {testint2}");
                    System.Diagnostics.Debug.WriteLine($"Creating a streaming locator named {streamingLocatorName}");
                    //IList<string> filters = new List<string>
                    //{
                    //    drvAssetFilterName
                    //};
                    //System.Diagnostics.Debug.WriteLine("filter made");

                    //StreamingLocator locator = await CreateStreamingLocatorAsync(client, RESOURCE_GROUP_NAME, ACCOUNT_NAME, assetName, drvStreamingLocatorName);




                    StreamingLocator locator = await client.StreamingLocators.CreateAsync(RESOURCE_GROUP_NAME,
                        ACCOUNT_NAME,
                        drvStreamingLocatorName,
                        new StreamingLocator
                        {
                            AssetName = liveOutput.AssetName,
                            StreamingPolicyName = PredefinedStreamingPolicy.DownloadAndClearStreaming,
                            //Filters = filters   // Associate the dvr filter with StreamingLocator.
                        });

                    System.Diagnostics.Debug.WriteLine("locator made");
                    StreamingEndpoint streamingEndpoint = await client.StreamingEndpoints.GetAsync(RESOURCE_GROUP_NAME, ACCOUNT_NAME, streamingEndpointName);

                    string jsonString2 = JsonSerializer.Serialize(streamingEndpoint);

                    System.Diagnostics.Debug.WriteLine(jsonString2);
                    System.Diagnostics.Debug.WriteLine($"streaming locatior Id {locator.StreamingLocatorId.ToString()}");
                    var hostname = streamingEndpoint.HostName;
                    var scheme = "https";
                    List<string> manifests = BuildManifestPaths(scheme, hostname, locator.StreamingLocatorId.ToString(), manifestName);
                    urls = manifests;
                    System.Diagnostics.Debug.WriteLine($"The HLS (MP4) manifest for the Live stream  : {manifests[0]}");
                    System.Diagnostics.Debug.WriteLine("Open the following URL to playback the live stream in an HLS compliant player (HLS.js, Shaka, ExoPlayer) or directly in an iOS device");
                    System.Diagnostics.Debug.WriteLine($"{manifests[0]}");
                    System.Diagnostics.Debug.WriteLine("");
                    System.Diagnostics.Debug.WriteLine($"The DASH manifest for the Live stream is : {manifests[1]}");
                    System.Diagnostics.Debug.WriteLine("Open the following URL to playback the live stream from the LiveOutput in the Azure Media Player");
                    System.Diagnostics.Debug.WriteLine($"https://ampdemo.azureedge.net/?url={manifests[1]}&heuristicprofile=lowlatency");
                    System.Diagnostics.Debug.WriteLine("");
                    System.Diagnostics.Debug.WriteLine("Continue experimenting with the stream until you are ready to finish.");
                    System.Diagnostics.Debug.WriteLine("Press enter to stop the LiveOutput...");
                }

                else
                {
                    string hostname = "";
                    //string locatorId = "";
                    var liveList = await client.LiveOutputs.ListAsync(RESOURCE_GROUP_NAME, ACCOUNT_NAME, LIVE_EVENT);
                    var locatorList = await client.StreamingLocators.ListAsync(RESOURCE_GROUP_NAME, ACCOUNT_NAME);
                    //System.Diagnostics.Debug.WriteLine(locatorList.StreamingLocatorId.ToString());
                    var endpointList = await client.StreamingEndpoints.ListAsync(RESOURCE_GROUP_NAME, ACCOUNT_NAME);

                    List<DateTime> datelist = new List<DateTime>();
                    var testDic = new Dictionary<DateTime, string>();

                    foreach (var item in endpointList)
                    {
                        System.Diagnostics.Debug.WriteLine(item.HostName.ToString());
                        System.Diagnostics.Debug.WriteLine(item.Location.ToString());
                        hostname = item.HostName.ToString();

                    }

                    foreach (var item in liveList)
                    {
                        System.Diagnostics.Debug.WriteLine(item.Id.ToString());

                    }

                    foreach (var item in locatorList)
                    {
                        System.Diagnostics.Debug.WriteLine(item.StreamingLocatorId.ToString());
                        System.Diagnostics.Debug.WriteLine(item.Created.ToString());
                        datelist.Add(item.Created);
                        testDic.Add(item.Created, item.StreamingLocatorId.ToString());

                    }

                    datelist.Sort((a, b) => b.CompareTo(a));

                    string locatorId = testDic[datelist[0]];
                    List<string> manifests = BuildManifestPaths("https", hostname, locatorId, "output");
                    //System.Diagnostics.Debug.WriteLine($"this is the id found in dic {teststring}");
                    urls = manifests;
                    System.Diagnostics.Debug.WriteLine($"The HLS (MP4) manifest for the Live stream  : {manifests[0]}");
                    System.Diagnostics.Debug.WriteLine("Open the following URL to playback the live stream in an HLS compliant player (HLS.js, Shaka, ExoPlayer) or directly in an iOS device");
                    System.Diagnostics.Debug.WriteLine($"{manifests[0]}");
                    System.Diagnostics.Debug.WriteLine("");
                    System.Diagnostics.Debug.WriteLine($"The DASH manifest for the Live stream is : {manifests[1]}");
                    //urls.Add("something");
                    //urls.Add("somethingelse");
                }
                




            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return Enumerable.Range(0, 1).Select(index => new MyStreamingLocator
            {
                Date = DateTime.Now,
                Summary = "teesting this string",
                url = urls[index]
            })
            .ToArray();
        }
    }


    public class Authentication
    {
        public static readonly string TokenType = "Bearer";

        /// <summary>
        /// Creates the AzureMediaServicesClient object based on the credentials
        /// supplied in local configuration file.
        /// </summary>
        /// <param name="config">The param is of type ConfigWrapper, which reads values from local configuration file.</param>
        /// <returns>A task.</returns>
        // <CreateMediaServicesClientAsync>
        public static async Task<IAzureMediaServicesClient> CreateMediaServicesClientAsync( bool interactive = false)
        {
            string RESOURCE = "https://management.core.windows.net/";
            // Tenant ID for your Azure Subscription (AadTenantId)
            string TENANT_ID = "f812b3f1-8163-4ccf-a222-bb8e9beee0af";
            // Your Service Principal App ID (AadClientId)
            string CLIENT = "8ddb8dbd-a8ef-4c7e-b8b9-76150566aa08";
            // Your Service Principal Password (AadSecret)
            string KEY = "-N-B~~h0~b2YdID~vGev145~T5GbOwQ56J";
            // Your Azure Subscription ID (SubscriptionId)
            string SUBSCRIPTION_ID = "d60b12fd-c2b9-4de4-8034-a7c52259a469";
            // Your Azure Media Service (AMS) Account Name
            //string ACCOUNT_NAME = "test0fire0account";
            // Your Resource Group Name
            //string RESOUCE_GROUP_NAME = "test_fire_stream";

            ServiceClientCredentials credentials;
            
            Uri ArmEndpoint = new Uri( "https://management.azure.com/");

            credentials = await GetCredentialsAsync(CLIENT, KEY, TENANT_ID, "https://management.core.windows.net/");

            return new AzureMediaServicesClient(ArmEndpoint, credentials)
            {
                SubscriptionId =SUBSCRIPTION_ID,
            };
        }
        // </CreateMediaServicesClientAsync>

        private static async Task<ServiceClientCredentials> GetCredentialsAsync(string AadClientId, string AadSecret, string AadTenantId, string ArmAadAudience)
        {
            // Use ConfidentialClientApplicationBuilder.AcquireTokenForClient to get a token using a service principal with symmetric key

            var scopes = new[] { ArmAadAudience + "/.default" };

            var app = ConfidentialClientApplicationBuilder.Create(AadClientId)
                .WithClientSecret(AadSecret)
                .WithAuthority(AzureCloudInstance.AzurePublic, AadTenantId)
                .Build();

            var authResult = await app.AcquireTokenForClient(scopes)
                                                     .ExecuteAsync()
                                                     .ConfigureAwait(false);

            return new TokenCredentials(authResult.AccessToken, TokenType);
        }

    }

}
