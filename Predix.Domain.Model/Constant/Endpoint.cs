namespace Predix.Domain.Model.Constant
{
    public class Endpoint
    {
        /// <summary>
#pragma warning disable 1570
        /// {{metadataurl}}/v2/locations/search?bbox={{long,lat}}&page={int}&size={{int}}&q=locationType:{{locationType1}}
#pragma warning restore 1570
        /// </summary>
        public const string GetListOfLocation =
            "{{metadataurl}}/v2/locations/search?bbox={{bbox}}&page={{pageNumber}}&size={{pageSize}}&q=locationType:{{locationType}}";
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