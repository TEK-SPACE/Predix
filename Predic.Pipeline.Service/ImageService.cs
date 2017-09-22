using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Predic.Pipeline.DataService;
using Predic.Pipeline.Interface;
using Predix.Domain.Model.Constant;
using Predix.Domain.Model.Location;

namespace Predic.Pipeline.Service
{
    public class ImageService : IImage
    {
        private readonly IPredixHttpClient _predixHttpClient;
        private static Dictionary<string, object> _globalVariables;
        public ImageService(Dictionary<string, object> globalVariables)
        {
            _predixHttpClient = new PredixHttpClient(globalVariables);
            _globalVariables = globalVariables;
        }

        public string MediaOnDemand(string imageAssetUid, string timestamp)
        {
            Image image = new Image();
            var media = GetMedia(imageAssetUid, timestamp);
            if (!string.IsNullOrWhiteSpace(media?.PollUrl))
            {
                Dictionary<string, string> additionalHeaders =
                    new Dictionary<string, string> {{"predix-zone-id", "SDSIM-IE-PUBLIC-SAFETY"}};
                var response = _predixHttpClient.GetAllAsync(media.PollUrl, additionalHeaders);
                if (!string.IsNullOrWhiteSpace(response.Result))
                {
                    var jsonRespone = JsonConvert.DeserializeObject<JObject>(response.Result);
                    image = jsonRespone != null
                        ? (jsonRespone).ToObject<Image>()
                        : new Image();
                }
            }
            if (image?.Entry == null || !image.Entry.Contents.Any()) return null;
            var imageBinary =  (from content in image.Entry.Contents
                let additionalHeaders = new Dictionary<string, string> {{"predix-zone-id", "SDSIM-IE-PUBLIC-SAFETY"}}
                select _predixHttpClient.GetFile(content.Url, additionalHeaders)
                into response
                select response.Result).FirstOrDefault();
            image.Base64 = imageBinary;

            image.ImageAssetUid = imageAssetUid;
            Save(image);
            return imageBinary;
        }

        private Media GetMedia(string imageAssetUid, string timestamp)
        {
            Media media = null;
            Dictionary<string, string> additionalHeaders =
                new Dictionary<string, string> {{"predix-zone-id", "SDSIM-IE-PUBLIC-SAFETY"}};
            var response = _predixHttpClient.GetAllAsync(Endpoint.MediaOnDemand
                .Replace("{ps_asset}", imageAssetUid)
                .Replace("{timestamp}", timestamp), additionalHeaders);
            if (string.IsNullOrWhiteSpace(response.Result)) return null;
            var jsonRespone = JsonConvert.DeserializeObject<JObject>(response.Result);
            media = jsonRespone != null
                ? (jsonRespone).ToObject<Media>()
                : new Media();
            media.ImageAssetUid = imageAssetUid;
            Save(media);
            return media;
        }

        private void Save(Media media)
        {
            if (media == null)
                return;
            using (PredixContext context = new PredixContext())
            {
                context.Medias.AddOrUpdate(x=>x.ImageAssetUid, media);
                context.SaveChanges();
            }
        }

        private void Save(Image image)
        {
            if (image == null)
                return;
            using (PredixContext context = new PredixContext())
            {
                context.Images.AddOrUpdate(x => x.ImageAssetUid, image);
                context.SaveChanges();
            }
        }
    }
}
