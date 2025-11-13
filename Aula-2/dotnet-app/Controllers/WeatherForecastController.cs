using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace WeatherApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    // Métricas customizadas do Prometheus
    private static readonly Counter RequestCounter = Metrics.CreateCounter(
        "weather_requests_total",
        "Total de requisições ao endpoint de previsão do tempo",
        new CounterConfiguration
        {
            LabelNames = new[] { "method", "endpoint" }
        });

    private static readonly Histogram RequestDuration = Metrics.CreateHistogram(
        "weather_request_duration_seconds",
        "Duração das requisições ao endpoint de previsão do tempo",
        new HistogramConfiguration
        {
            LabelNames = new[] { "method", "endpoint" },
            Buckets = Histogram.LinearBuckets(start: 0.01, width: 0.01, count: 10)
        });

    private static readonly Gauge ActiveRequests = Metrics.CreateGauge(
        "weather_active_requests",
        "Número de requisições ativas no momento");

    private static readonly Summary TemperatureSummary = Metrics.CreateSummary(
        "weather_temperature_celsius",
        "Resumo das temperaturas geradas");

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        // Incrementar contador de requisições
        RequestCounter.WithLabels("GET", "/WeatherForecast").Inc();

        // Medir duração da requisição
        using (RequestDuration.WithLabels("GET", "/WeatherForecast").NewTimer())
        {
            // Incrementar gauge de requisições ativas
            ActiveRequests.Inc();

            try
            {
                _logger.LogInformation("Gerando previsão do tempo");

                var forecast = Enumerable.Range(1, 5).Select(index =>
                {
                    var temp = Random.Shared.Next(-20, 55);
                    
                    // Registrar temperatura no summary
                    TemperatureSummary.Observe(temp);

                    return new WeatherForecast
                    {
                        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        TemperatureC = temp,
                        Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                    };
                })
                .ToArray();

                return forecast;
            }
            finally
            {
                // Decrementar gauge de requisições ativas
                ActiveRequests.Dec();
            }
        }
    }

    [HttpGet("slow", Name = "GetSlowWeatherForecast")]
    public async Task<IEnumerable<WeatherForecast>> GetSlow()
    {
        // Endpoint lento para demonstrar métricas de latência
        RequestCounter.WithLabels("GET", "/WeatherForecast/slow").Inc();

        using (RequestDuration.WithLabels("GET", "/WeatherForecast/slow").NewTimer())
        {
            ActiveRequests.Inc();

            try
            {
                _logger.LogWarning("Executando endpoint lento (simulação)");

                // Simular processamento lento
                await Task.Delay(Random.Shared.Next(1000, 3000));

                return Enumerable.Range(1, 5).Select(index =>
                {
                    var temp = Random.Shared.Next(-20, 55);
                    TemperatureSummary.Observe(temp);

                    return new WeatherForecast
                    {
                        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        TemperatureC = temp,
                        Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                    };
                })
                .ToArray();
            }
            finally
            {
                ActiveRequests.Dec();
            }
        }
    }

    [HttpGet("error", Name = "GetErrorWeatherForecast")]
    public IActionResult GetError()
    {
        // Endpoint que gera erro para demonstrar métricas de erro
        RequestCounter.WithLabels("GET", "/WeatherForecast/error").Inc();

        _logger.LogError("Endpoint de erro foi chamado");

        return StatusCode(500, new { error = "Erro simulado para demonstração de métricas" });
    }
}
