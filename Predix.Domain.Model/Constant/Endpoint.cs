using System;
using System.Configuration;
using System.Text;

namespace Predix.Domain.Model.Constant
{
    public abstract class Endpoint : GeorgiaPowerAtlantaEndpoints
    {
        public static readonly string AccessTokenUri = $"{UaaUrl}/oauth/token?grant_type=client_credentials";

        public static readonly string PkInPkOutByLocationId =
                $"{Eventurl}/locations/{{parking_loc}}/events?eventType={{parkInOrOut}}&startTime={{startTimeInEpoch}}&endTime={{endTimeInEpoch}}";
        public static string ClientAccessToken { get; set; }
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
        public static readonly string GetListOfLocation =
            $"{MetaDataUrl}/v2/metadata/locations/search?q=locationType:{{locationType}}&bbox={{bbox}}&page={{pageNumber}}&size={{pageSize}}";

        public static readonly string GetLocationDetails =
            $"{MetaDataUrl}/v2/metadata/locations/{{locationUid}}";

        public static readonly string MediaOnDemand = $"{MediaUrl}/ondemand/assets/{{ps_asset}}/media?mediaType=IMAGE&timestamp={{timestamp}}&page=0&size=100&sortBy=mediaLogId&sortDir=DESC";
    }
}