namespace MessageBus.Examples.MessageBus.Weather
{
    [Topic("Queries/GetCurrentWeather")]
    public sealed class WeatherQuery : IMessageQuery<WeatherQuery.Result>
    {
        public MessageId MessageId { get; } = MessageId.NewId();

        public sealed class Result : IMessageQueryResult
        {
            public Result(MessageId messageId, string weather)
            {
                MessageId = messageId;
                Weather = weather;
            }

            public MessageId MessageId { get; }

            public string Weather { get; }
        }
    }
}
