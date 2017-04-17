using System;
using System.Collections.Generic;
using Predic.Pipeline.Interface;
using Predix.Domain.Model.Location;

namespace Predic.Pipeline.Service
{
    public class LocationService : ILocation, IImage
    {
        public List<Identifier> All(string bbox, string locationType, int size, int page)
        {
            throw new NotImplementedException();
        }

        public Details Get(string locationUid, string eventType)
        {
            throw new NotImplementedException();
        }

        public void SaveLocationKeys(List<Identifier> locationKeys)
        {
            throw new NotImplementedException();
        }

        public void SaveLocationDetails(List<Details> locationDetails)
        {
            throw new NotImplementedException();
        }

        public Image MediaOnDemand(string imageAssetUid)
        {
            throw new NotImplementedException();
        }

        public void SaveImage(Image image)
        {
            throw new NotImplementedException();
        }
    }
}
