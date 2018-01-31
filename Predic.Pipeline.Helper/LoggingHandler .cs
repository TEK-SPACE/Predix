using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Predix.Domain.Model;
using Predix.Domain.Model.Enum;
using Predix.Pipeline.DataService;

namespace Predix.Pipeline.Helper
{
    public class LoggingHandler : DelegatingHandler
    {
        public int ActivityId { get; set; }
        public LoggingHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            //Console.WriteLine("Request:");
            //Console.WriteLine(request.ToString());
            if (request.Content != null)
            {
                //Console.WriteLine(await request.Content.ReadAsStringAsync());
            }
            //Console.WriteLine();

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            //Console.WriteLine("Response:");
            //Console.WriteLine(response.ToString());
            if (response.Content != null)
            {
                //Console.WriteLine(await response.Content.ReadAsStringAsync());
            }
            //Console.WriteLine();

            using (PredixContext context = new PredixContext())
            {
                var activity = new Activity
                {
                    ProcessDateTime = DateTime.Now,
                    RequestJson = request.Content?.ReadAsStringAsync().Result,
                    ResponseJson = response.Content?.ReadAsStringAsync().Result,
                    Type = request.Method.Method.Equals("get", StringComparison.OrdinalIgnoreCase)
                        ? ActivityType.Get
                        : ActivityType.Post
                };
                context.Activities.Add(activity);
                context.SaveChanges();
                ActivityId = activity.Id;
            }
            return response;
        }
    }
}
