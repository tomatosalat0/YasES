using System.Net.Http;
using System.Threading.Tasks;

namespace MessageBus.Examples.MessageBus.Weather
{

    public class WttrInQueryHandler : IAsyncMessageQueryHandler<WeatherQuery, WeatherQuery.Result>
    {
        public async Task<WeatherQuery.Result> HandleAsync(WeatherQuery query)
        {
            string response = await FetchWeather();
            return new WeatherQuery.Result(query.MessageId, response);
        }

        private async Task<string> FetchWeather()
        {
            using HttpClient client = new HttpClient();

            HttpResponseMessage response = await client.GetAsync("https://wttr.in/?FA");
            string rawContent = await response.EnsureSuccessStatusCode()
                .Content
                .ReadAsStringAsync();

            return rawContent;
        }
    }
}
