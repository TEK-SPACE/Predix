using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Predix.Domain.Model.Constant;
using Predix.Domain.Model.Location;
using Predix.Pipeline.DataService;
using Predix.Pipeline.Helper;
using Predix.Pipeline.Interface;
using Image = Predix.Domain.Model.Location.Image;

namespace Predix.Pipeline.Service
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

        public void MediaOnDemand(ParkingEvent parkingEvent, string imageAssetUid, string timestamp)
        {
            var media = GetMedia(imageAssetUid, timestamp);
            Image image = new Image { PropertyId = parkingEvent.Properties.Id };
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
                        try
                        {
                            var jsonRespone = JsonConvert.DeserializeObject<JObject>(response.Result);
                            image = jsonRespone != null
                                ? (jsonRespone).ToObject<Image>()
                                : new Image();
                        }
                        catch (Exception e)
                        {
                            Commentary.Print(e.Message);
                            Commentary.Print(response.Result);
                        }
                       
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
                image = MarkPixelCoordinates(parkingEvent: parkingEvent, image: image);
            }
            if (image == null) return;
            image.PropertyId = parkingEvent.Properties.Id;
            image.ImageAssetUid = imageAssetUid;
            Save(image);
            //return image.Base64;
        }

        private static Bitmap Base64StringToBitmap(string base64String)
        {
            Bitmap bmpReturn = null;
            byte[] byteBuffer = Convert.FromBase64String(base64String);
            MemoryStream memoryStream = new MemoryStream(byteBuffer) {Position = 0};
            bmpReturn = (Bitmap) System.Drawing.Image.FromStream(memoryStream);
            memoryStream.Close();
            memoryStream = null;
            byteBuffer = null;
            return bmpReturn;
        }

        private Image MarkPixelCoordinates(ParkingEvent parkingEvent, Image image)
        {
            try
            {
                image.OriginalBase64 = image.Base64;
                if (!string.IsNullOrWhiteSpace(parkingEvent.Properties.PixelCoordinates))
                {
                    var bitMapImage = Base64StringToBitmap(image.Base64);
                    var coordinates = parkingEvent.Properties.PixelCoordinates.Split(',').ToList();
                    foreach (var coordinate in coordinates)
                    {
                        var xys = coordinate.Split(':').Select(int.Parse).ToList();
                        bitMapImage.SetPixel(xys[0], xys[1], Color.DarkSalmon);
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitMapImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        byte[] byteImage = ms.ToArray();
                        image.Base64 = Convert.ToBase64String(byteImage); //Get Base64
                    }
                }
            }
            catch (Exception e)
            {
                Commentary.Print(
                    $"Failed to mark pixel coordinates {parkingEvent.Properties.PixelCoordinates} PropertyId {parkingEvent.Properties.Id}");
                Commentary.Print(e.Message);
                return image;
            }

            return image;
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
                context.Medias.Add(media);
                //context.Medias.AddOrUpdate(x => x.ImageAssetUid, media);
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
                context.ImageContents.AddRange(image.Entry.Contents);
                context.Images.Add(image);
                //context.Images.AddOrUpdate(x => x.ImageAssetUid, image);
                context.SaveChanges();
            }
        }
    }
}
