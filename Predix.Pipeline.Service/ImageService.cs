using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Drawing;
using System.Drawing.Imaging;
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
            byte[] byteBuffer = Convert.FromBase64String(base64String);
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
                    var bitMapImage = Base64StringToBitmap(selectedBase64.Trim());
                    var coordinates = parkingEvent.Properties.PixelCoordinates.Split(',').ToList();
                    using (var graphics = Graphics.FromImage(bitMapImage))
                    {
                        Pen yellowPen = new Pen(Color.Yellow, 3);
                        try
                        {
                            var a1 = coordinates[0].Split(':').Select(float.Parse).ToList()[0];
                            var a2 = coordinates[0].Split(':').Select(float.Parse).ToList()[1];
                            var b1 = coordinates[1].Split(':').Select(float.Parse).ToList()[0];
                            var b2 = coordinates[1].Split(':').Select(float.Parse).ToList()[1];
                            var c1 = coordinates[2].Split(':').Select(float.Parse).ToList()[0];
                            var c2 = coordinates[2].Split(':').Select(float.Parse).ToList()[1];
                            var d1 = coordinates[3].Split(':').Select(float.Parse).ToList()[0];
                            var d2 = coordinates[3].Split(':').Select(float.Parse).ToList()[1];
                            graphics.DrawPolygon(yellowPen, new PointF[]
                            {
                                new PointF(a1, a2),
                                new PointF(b1, b2),
                                new PointF(c1, c2),
                                new PointF(d1, d2)
                            });
                            // Create font and brush.
                            Font drawFont = new Font("Arial", 26);
                            SolidBrush drawBrush = new SolidBrush(Color.White);

                            // Create point for upper-left corner of drawing.
                            PointF drawPoint = new PointF(150.0F, 110.0F);
                            //StringFormat stringFormat = new StringFormat
                            //{
                            //    LineAlignment = StringAlignment.Center,
                            //    Alignment = StringAlignment.Center
                            //};
                            //graphics.DrawString($"Event Id: {parkingEvent.Id}, Asset Uid: {parkingEvent.AssetUid}, Pixel Coordinates {parkingEvent.Properties.PixelCoordinates}",
                            //    drawFont, drawBrush, drawPoint);
                            List<string> writings = new List<string>
                            {
                                $"Event Id: {parkingEvent.Id}",
                                $"In or Out: {parkingEvent.EventType}",
                                $"Timestamp: {parkingEvent.Timestamp} {parkingEvent.EventTime?.ToString("F")}",
                                $"Location Uid: {parkingEvent.LocationUid}",
                                $"Asset Uid: {parkingEvent.AssetUid}",
                                $"Pixel Coordinates {parkingEvent.Properties.PixelCoordinates}"
                            };
                            foreach (var writing in writings)
                            {
                                drawPoint.Y = drawPoint.Y + 40;
                                graphics.DrawString(writing,
                                    drawFont, drawBrush, drawPoint);
                            }
                        }
                        catch (Exception e)
                        {
                            Commentary.Print(
                                $"Failed to mark pixel coordinates {parkingEvent.Properties.PixelCoordinates} PropertyId {parkingEvent.Properties.Id}");
                            Commentary.Print(e.Message);
                        }

                    }

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        string randomTempFileName = Path.GetTempPath();
                        if(!Directory.Exists($"{randomTempFileName}\\CivicSmart\\{DateTime.Now:yyyy-MM-dd}"))
                        {
                            Directory.CreateDirectory($"{randomTempFileName}\\CivicSmart\\{DateTime.Now:yyyy-MM-dd}");
                        }
                        using (FileStream fs = new FileStream($"{randomTempFileName}\\CivicSmart\\{DateTime.Now:yyyy-MM-dd}\\geviolation{DateTime.Now.Ticks}.jpg",
                            FileMode.Create, FileAccess.ReadWrite))
                        {
                            bitMapImage.Save(memoryStream, ImageFormat.Jpeg);
                            byte[] bytes = memoryStream.ToArray();
                            fs.Write(bytes, 0, bytes.Length);
                            image.Base64 = image.Base64.Split(',').ToList<string>()[0] + "," +
                                           Convert.ToBase64String(bytes);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Commentary.Print(
                    $"Failed to convert base64 to Image {image.Base64} PropertyId {parkingEvent.Properties.Id}");
                Commentary.Print(e.Message);
                return image;
            }

            return image;
        }

        public List<Tuple<int, string, string>> GetRecentBase64()
        {
            using (PredixContext context = new PredixContext())
            {
                context.Database.CommandTimeout = 1200;
                var results = (from pep in context.ParkingEventProperties
                    join i in context.Images on pep.ImageAssetUid equals i.ImageAssetUid
                    select new
                    {
                        pep.Id,
                        pep.PixelCoordinates,
                        i.Base64
                    }).Take(5).ToList().Select(f => new Tuple<int, string, string>(
                    f.Id,
                    f.PixelCoordinates,
                    f.Base64
                )).ToList();
                return results;
            }
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
