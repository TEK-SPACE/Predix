namespace Predix.Domain.Model.Constant
{
    public class StagingEndpoints
    {
        /// <summary>
        /// MetaData Services URL (Traffic & Pedestrian)
        /// </summary>
        protected const string MetaDataUrl = "https://ic-metadata-service.run.aws-usw02-pr.ice.predix.io/v2/metadata";

        /// <summary>
        /// UAA URL (Client Authentication)
        /// </summary>
        protected const string UaaUrl =
            "https://890407d7-e617-4d70-985f-01792d693387.predix-uaa.run.aws-usw02-pr.ice.predix.io";

        /// <summary>
        /// Event Services URL (Parking)
        /// </summary>
        protected const string Eventurl = "https://ie-cities-events.run.asv-pr-pub.ice.predix.io/v2";

        /// <summary>
        /// Websocket URL (Access for realtime)
        /// </summary>
        public const string WebSocketUrlForEvents =
            "wss://ic-websocket-service.run.aws-usw02-pr.ice.predix.io/events";

        /// <summary>
        /// Media Services URL (Images and video)
        /// </summary>
        protected const string MediaUrl = "https://ic-media-service.run.aws-usw02-pr.ice.predix.io/v2/mediastore";

        /// <summary>
        /// wss://{{production url}}/events
        /// <para>{“locationUid":"{{location-UID}}","eventTypes":[" {{eventType1}},{{eventType2}}"]}
        /// <example>{“locationUid":"LOCATION-282","eventTypes":["PKIN,PKOUT"]}</example></para>
        /// </summary>
        protected const string NearRealTimeDataByLocationUid = "wss://<production url>/events";
    }
}