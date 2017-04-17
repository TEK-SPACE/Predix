using System;
using System.ComponentModel.DataAnnotations;
using Predix.Domain.Model.Enum;

namespace Predix.Domain.Model
{
    public class Activity
    {
        [Key]
        public int Id { get; set; }
        public ActivityType Type { get; set; }
        public DateTime ProcessDateTime { get; set; }
        public string RequestJson { get; set; }
        public string ResponseJson { get; set; }
        public string Error { get; set; }
        public int RetryCount { get; set; }
    }
}
