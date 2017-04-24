using System;
using System.Configuration;
using System.Text;

namespace Predix.Domain.Model.Constant
{
    public class Endpoint
    {

        public const string metaDataUrl = "https://ie-cities-metadata.run.asv-pr-pub.ice.predix.io/v2";
        public const string BaseUrl =
            "https://8553482c-1d32-4d38-8597-2e56ab642dd3.predix-uaa.run.asv-pr.ice.predix.io";
        public static readonly string AccessTokenUri = $"{BaseUrl}/oauth/token?grant_type=client_credentials";

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

        public const string WebSocketUrl = "wss://ie-cities-websocket.run.asv-pr-pub.ice.predix.io/events";
        public static string ClientAccessToken { get; set; }

        public static readonly string GetListOfLocation =
                $"{metaDataUrl}/locations/search?q=locationType:{{locationType}}&bbox={{bbox}}&page={{pageNumber}}&size={{pageSize}}"
            ;

        /// <summary>
        /// wss://{{production url}}/events
        /// <para>{“locationUid":"{{location-UID}}","eventTypes":[" {{eventType1}},{{eventType2}}"]}
        /// <example>{“locationUid":"LOCATION-282","eventTypes":["PKIN,PKOUT"]}</example></para>
        /// </summary>
        public const string NearRealTimeDataByLocationUid = "wss://<production url>/events";
        /// <summary>
        /// <para>"headers": "Authorization: Bearer {{client_token}}\nPredix-Zone-Id: {{ps_zone_id}}\n"</para>
        /// <para>"method": "GET"</para>
        /// <para>"data": []</para>
        /// <para>"version": 2</para>
        /// </summary>
        public const string MediaOnDemand =
                "{{mediaurl}}/ondemand/assets/{{ps_asset}}/media?mediaType=IMAGE&timestamp=1480503600000&page=0&size=100&sortBy=mediaLogId&sortDir=DESC";
    }
}