using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace PBServices.Carriers.ZipShip;

/// <summary>
/// Example of our house carrier-integration pattern. The <c>generate</c> command uses
/// files like this as a few-shot example so drafts follow our naming, DI, logging and
/// exception conventions.
/// </summary>
public sealed class ZipShipClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ZipShipClient> _logger;

    public ZipShipClient(HttpClient httpClient, ILogger<ZipShipClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ZipShipLabelResponse> CreateLabelAsync(
        ZipShipLabelRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating ZipShip label for reference {Reference}", request.Reference);

        using var response = await _httpClient.PostAsJsonAsync("labels", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var code = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("ZipShip label creation failed with status {Status}: {Body}", response.StatusCode, code);
            throw ZipShipErrorMapping.ToException(code);
        }

        var result = await response.Content.ReadFromJsonAsync<ZipShipLabelResponse>(cancellationToken: cancellationToken)
            ?? throw new CarrierIntegrationException("ZipShip returned an empty response.");

        _logger.LogInformation("Created ZipShip label {LabelId}", result.LabelId);
        return result;
    }
}

public sealed class CarrierIntegrationException : Exception
{
    public CarrierIntegrationException(string message) : base(message)
    {
    }

    public CarrierIntegrationException(string code, string message) : base(message) => Code = code;

    public string? Code { get; }
}
