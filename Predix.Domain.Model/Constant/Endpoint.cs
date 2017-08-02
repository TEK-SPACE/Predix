using System;
using System.Configuration;
using System.Text;

namespace Predix.Domain.Model.Constant
{
    public static class Endpoint
    {
        private const string MetaDataUrl = "https://ic-metadata-service.run.aws-usw02-pr.ice.predix.io/v2/metadata";

        private const string BaseUrl =
            "https://890407d7-e617-4d70-985f-01792d693387.predix-uaa.run.aws-usw02-pr.ice.predix.io";
        public static readonly string AccessTokenUri = $"{BaseUrl}/oauth/token?grant_type=client_credentials";

        private const string Eventurl = "https://ie-cities-events.run.asv-pr-pub.ice.predix.io/v2";

        public static readonly string PkInPkOutByLocationId =
                $"{Eventurl}/locations/{{parking_loc}}/events?eventType={{parkInOrOut}}&startTime={{startTimeInEpoch}}&endTime={{endTimeInEpoch}}";

        public static string OAuthToken
        {
            get
            {
                var bytes = Encoding.UTF8.GetBytes(
                    $"{ConfigurationManager.AppSettings["ClientId"]}:{ConfigurationManager.AppSettings["ClientSecrete"]}");
                var base64 = Convert.ToBase64String(bytes);
                return base64;
            }
        }

        public const string WebSocketUrlForEvents = "wss://ic-websocket-server.run.aws-usw02-pr.ice.predix.io/events";
        public static string ClientAccessToken { get; set; }

        public static readonly string GetListOfLocation =
                $"{MetaDataUrl}/locations/search?q=locationType:{{locationType}}&bbox={{bbox}}&page={{pageNumber}}&size={{pageSize}}";

        public const string MediaUrl = "https://ic-media-service.run.aws-usw02-pr.ice.predix.io/v2/mediastore";

        /// <summary>
        /// wss://{{production url}}/events
        /// <para>{“locationUid":"{{location-UID}}","eventTypes":[" {{eventType1}},{{eventType2}}"]}
        /// <example>{“locationUid":"LOCATION-282","eventTypes":["PKIN,PKOUT"]}</example></para>
        /// </summary>
        public const string NearRealTimeDataByLocationUid = "wss://<production url>/events";

        public static readonly string MediaOnDemand = $"{MediaUrl}/ondemand/assets/{{ps_asset}}/media?mediaType=IMAGE&timestamp={{timestamp}}&page=0&size=100&sortBy=mediaLogId&sortDir=DESC";
    }
}