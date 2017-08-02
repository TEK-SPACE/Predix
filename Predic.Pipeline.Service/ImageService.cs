using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Predic.Pipeline.Interface;
using Predix.Domain.Model.Constant;
using Predix.Domain.Model.Location;

namespace Predic.Pipeline.Service
{
    public class ImageService : IImage
    {
        private readonly IPredixHttpClient _predixHttpClient = new PredixHttpClient();
        public Image MediaOnDemand(string imageAssetUid, string timestamp)
        {
            Image image = new Image();
            var media = GetMedia(imageAssetUid, timestamp);
            if (!string.IsNullOrWhiteSpace(media?.PollUrl))
            {
                Dictionary<string, string> additionalHeaders =
                    new Dictionary<string, string> { { "predix-zone-id", "SDSIM-IE-PUBLIC-SAFETY" } };
                var response = _predixHttpClient.GetAllAsync(media.PollUrl, additionalHeaders);
                if (!string.IsNullOrWhiteSpace(response.Result))
                {
                    var jsonRespone = JsonConvert.DeserializeObject<JObject>(response.Result);
                    image = jsonRespone != null
                        ? (jsonRespone).ToObject<Image>()
                        : new Image();
                }
            }
            if (image?.Entry != null && image.Entry.Contents.Any())
            {
                foreach (var content in image.Entry.Contents)
                {
                    Dictionary<string, string> additionalHeaders =
                        new Dictionary<string, string> { { "predix-zone-id", "SDSIM-IE-PUBLIC-SAFETY" } };
                    var response = _predixHttpClient.GetAllAsync(content.Url, additionalHeaders);
                    if (!string.IsNullOrWhiteSpace(response.Result))
                    {
                        var jsonRespone = JsonConvert.DeserializeObject<JObject>(response.Result);
                        image = jsonRespone != null
                            ? (jsonRespone).ToObject<Image>()
                            : new Image();
                    }
                }
            }
            return image;
        }

        private Media GetMedia(string imageAssetUid, string timestamp)
        {
            Media media = null;
            Dictionary<string, string> additionalHeaders =
                new Dictionary<string, string> {{"predix-zone-id", "SDSIM-IE-PUBLIC-SAFETY" } };
            var response = _predixHttpClient.GetAllAsync(Endpoint.MediaOnDemand
                .Replace("{ps_asset}", imageAssetUid)
                .Replace("{timestamp}", timestamp), additionalHeaders);
            if (!string.IsNullOrWhiteSpace(response.Result))
            {
                var jsonRespone = JsonConvert.DeserializeObject<JObject>(response.Result);
                media = jsonRespone != null
                    ? (jsonRespone).ToObject<Media>()
                    : new Media();
            }
            return media;
        }

        public void SaveImage(Image image)
        {
            throw new NotImplementedException();
        }
    }
}
