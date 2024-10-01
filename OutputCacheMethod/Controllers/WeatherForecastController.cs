using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using System.Text;
using System.Text.Json;

namespace OutputCacheMethod.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IOutputCacheStore outputCacheStore;

        public WeatherForecastController(ILogger<WeatherForecastController> logger,
            IOutputCacheStore outputCacheStore)
        {
            _logger = logger;
            this.outputCacheStore = outputCacheStore;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            _logger.LogInformation("Executing GetWeatherForecast");

            var key = "weatherforecasts-get-all";

            var dataFromCacheStore = await outputCacheStore.GetAsync(key, default);

            if (dataFromCacheStore is not null)
            {
                var dataDeserialized = ConvertFromBytesToObject<IEnumerable<WeatherForecast>>(dataFromCacheStore);
                return dataDeserialized;
            }


            await Task.Delay(3000);
            var value =  Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();

            var valueToStoreInCache = ConvertFromObjectToBytes(value);

            await outputCacheStore.SetAsync(key, valueToStoreInCache, tags: null,
                validFor: GlobalValues.CacheExpirationTime, default);

            return value;
        }

        private T ConvertFromBytesToObject<T>(byte[] bytes)
        {
            var json = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<T>(json)!;
        }

        private byte[] ConvertFromObjectToBytes(object obj)
        {
            var json = JsonSerializer.Serialize(obj);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}
