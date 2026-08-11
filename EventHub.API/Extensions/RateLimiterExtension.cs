using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace EventHub.API.Extensions
{
    public static class RateLimiterExtension
    {
        public static IServiceCollection AddFixedRateLimiter(this IServiceCollection services)
        {
            services.AddRateLimiter(options => {

                options.AddFixedWindowLimiter("fixed", opt =>
                {
                    opt.PermitLimit = 1; // Maximum number of requests allowed
                    opt.Window = TimeSpan.FromSeconds(10); // Time window for the limit
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst; // Order of processing queued requests
                    opt.QueueLimit = 2; // Maximum number of requests that can be queued
                });

                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = 429; // Too Many Requests
                    await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", cancellationToken);
                };
            });
            return services;
        }   
    }
}
