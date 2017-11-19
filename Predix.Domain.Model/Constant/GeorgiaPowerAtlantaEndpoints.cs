namespace Predix.Domain.Model.Constant
{
    public class GeorgiaPowerAtlantaEndpoints
    {
        /// <summary>
        /// MetaData Services URL (Traffic & Pedestrian)
        /// </summary>
        protected const string MetaDataUrl = "https://ic-metadata-service-atlanta.run.aws-usw02-pr.ice.predix.io";

        /// <summary>
        /// UAA URL (Client Authentication)
        /// </summary>
        protected const string UaaUrl =
            "https://624eff02-dbb1-4c6c-90bc-fa85a29e5fa8.predix-uaa.run.aws-usw02-pr.ice.predix.io";

        /// <summary>
        /// Event Services URL (Parking)
        /// </summary>
        protected const string Eventurl = "https://ic-event-service-atlanta.run.aws-usw02-pr.ice.predix.io";

        /// <summary>
        /// Websocket URL (Access for realtime)
        /// </summary>
        public const string WebSocketUrlForEvents =
            "https://ic-websocket-service-atlanta.run.aws-usw02-pr.ice.predix.io";

        /// <summary>
        /// Media Services URL (Images and video)
        /// </summary>
        protected const string MediaUrl = "https://ic-media-service-atlanta.run.aws-usw02-pr.ice.predix.io";

        /// <summary>
        /// wss://{{production url}}/events
        /// <para>{“locationUid":"{{location-UID}}","eventTypes":[" {{eventType1}},{{eventType2}}"]}
        /// <example>{“locationUid":"LOCATION-282","eventTypes":["PKIN,PKOUT"]}</example></para>
        /// </summary>
        public const string NearRealTimeDataByLocationUid = "wss://<production url>/events";
    }
}