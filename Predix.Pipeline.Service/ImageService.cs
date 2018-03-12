using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
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

        public static bool IsBase64(string base64String)
        {
            if (string.IsNullOrEmpty(base64String) || base64String.Length % 4 != 0
                || base64String.Contains(" ") || base64String.Contains("\t") || base64String.Contains("\r") || base64String.Contains("\n"))
                return false;

            try
            {
                var fromBase64String = Convert.FromBase64String(base64String);
                return true;
            }
#pragma warning disable 168
            catch (Exception exception)
#pragma warning restore 168
            {
                // Handle the exception
            }
            return false;
        }
        private static Bitmap Base64StringToBitmap(string base64String)
        {
            //int mod4 = base64String.Length % 4;
            //if (mod4 > 0)
            //{
            //    base64String += new string('=', 4 - mod4);
            //}


            base64String = base64String.Remove(base64String.Length - 1, 1);

            byte[] byteBuffer = Convert.FromBase64String(base64String);
            //System.Drawing.Image image;
            //using (MemoryStream ms = new MemoryStream(byteBuffer))
            //{
            //    image = System.Drawing.Image.FromStream(ms);

            //    image.Save("c://zz.jpg",ImageFormat.Jpeg);
            //}
          
            using (MemoryStream memoryStream = new MemoryStream(byteBuffer))
            {
                var bmpReturn = (Bitmap) System.Drawing.Image.FromStream(memoryStream);
                memoryStream.Close();
                return bmpReturn;
            }
        }

        public Image MarkPixelCoordinates(ParkingEvent parkingEvent, Image image)
        {
            try
            {
                image.OriginalBase64 = image.Base64;
                if (!string.IsNullOrWhiteSpace(parkingEvent.Properties.PixelCoordinates))
                {
                    //IsBase64(image.Base64);
                    string selectedBase64 = image.Base64.Split(',').ToList<string>()[1];
                    var bitMapImage = Base64StringToBitmap(selectedBase64);
                    var coordinates = parkingEvent.Properties.PixelCoordinates.Split(',').ToList();
                    using (var graphics = Graphics.FromImage(bitMapImage))
                    {
                        Pen blackPen = new Pen(Color.Black, 3);
                        graphics.DrawPolygon(blackPen, new PointF[]
                        {
                            new PointF(coordinates[0].Split(':').Select(float.Parse).ToList()[0],
                                coordinates[0].Split(':').Select(int.Parse).ToList()[1]),
                            new PointF(coordinates[1].Split(':').Select(int.Parse).ToList()[0],
                                coordinates[1].Split(':').Select(int.Parse).ToList()[1]),
                            new PointF(coordinates[2].Split(':').Select(float.Parse).ToList()[0],
                                coordinates[2].Split(':').Select(int.Parse).ToList()[1]),
                            new PointF(coordinates[3].Split(':').Select(int.Parse).ToList()[0],
                                coordinates[3].Split(':').Select(int.Parse).ToList()[1])
                        });
                    }

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        //using (FileStream fs = new FileStream(@"C:\Users\hbopuri\AppData\Local\Temp\geviolation.jpg", FileMode.Create, FileAccess.ReadWrite))
                        //{
                            bitMapImage.Save(memoryStream, ImageFormat.Jpeg);
                            byte[] bytes = memoryStream.ToArray();
                            //fs.Write(bytes, 0, bytes.Length);
                            image.Base64 = image.Base64.Split(',').ToList<string>()[0] +
                                           Convert.ToBase64String(bytes); //Get Base64
                        //}
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
