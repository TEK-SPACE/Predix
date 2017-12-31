using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Predic.Pipeline.DataService;
using Predic.Pipeline.Helper;
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

        public void MediaOnDemand(string imageAssetUid, string timestamp)
        {
            var media = GetMedia(imageAssetUid, timestamp);
            Image image = new Image();
            var i = 1;
            while (i < 5 && (image?.Entry == null || !image.Entry.Contents.Any(x => x.Status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))))
            {
                i++;
                if (!string.IsNullOrWhiteSpace(media?.PollUrl))
                {
                    Dictionary<string, string> additionalHeaders =
                        new Dictionary<string, string> { { "predix-zone-id", Endpoint.PredixZoneIdForImage } };
                    var response = _predixHttpClient.GetAllAsync(media.PollUrl, additionalHeaders);
                    if (!string.IsNullOrWhiteSpace(response.Result))
                    {
                        var jsonRespone = JsonConvert.DeserializeObject<JObject>(response.Result);
                        image = jsonRespone != null
                            ? (jsonRespone).ToObject<Image>()
                            : new Image();
                    }
                }
                if (image?.Entry == null ||
                    !image.Entry.Contents.Any(x => x.Status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase)))
                {
                    Commentary.Print($"Polling for Media Data {i} time", true);
                    System.Threading.Thread.Sleep(1000 * 60);
                    continue;
                }
                var imageBinary = (from content in image.Entry.Contents
                                   let additionalHeaders = new Dictionary<string, string> { { "predix-zone-id", Endpoint.PredixZoneIdForImage } }
                                   select _predixHttpClient.GetFile(content.Url, additionalHeaders)
                    into response
                                   select response.Result).FirstOrDefault();
                image.Base64 = imageBinary;
            }
            if (image == null) return;
            image.ImageAssetUid = imageAssetUid;
            Save(image);
            //return image.Base64;
        }

        private Media GetMedia(string imageAssetUid, string timestamp)
        {
            Commentary.Print($"Fething Media Data", true);
            Dictionary<string, string> additionalHeaders =
                new Dictionary<string, string> {{"predix-zone-id", Endpoint.PredixZoneIdForImage } };
            var response = _predixHttpClient.GetAllAsync(Endpoint.MediaOnDemand
                .Replace("{ps_asset}", imageAssetUid)
                .Replace("{timestamp}", timestamp), additionalHeaders);
            if (string.IsNullOrWhiteSpace(response.Result)) return null;
            var jsonRespone = JsonConvert.DeserializeObject<JObject>(response.Result);
            var media = jsonRespone != null
                ? (jsonRespone).ToObject<Media>()
                : new Media();
            media.ImageAssetUid = imageAssetUid;
            Save(media);
            return media;
        }

        private async Task SaveAsync(Media media)
        {
            if (media == null)
                return;
            using (PredixContext context = new PredixContext())
            {
                Commentary.Print($"Saving Media Data");
                context.Medias.AddOrUpdate(x => x.ImageAssetUid, media);
                await context.SaveChangesAsync();
            }
        }
        private void Save(Media media)
        {
            if (media == null)
                return;
            using (PredixContext context = new PredixContext())
            {
                Commentary.Print($"Saving Media Data", true);
                context.Medias.AddOrUpdate(x => x.ImageAssetUid, media);
                context.SaveChanges();
            }
        }

        private void Save(Image image)
        {
            if (image == null)
                return;
            using (PredixContext context = new PredixContext())
            {
                Commentary.Print($"Saving Base64", true);
                context.Images.AddOrUpdate(x => x.ImageAssetUid, image);
                context.SaveChanges();
            }
        }
    }
}
