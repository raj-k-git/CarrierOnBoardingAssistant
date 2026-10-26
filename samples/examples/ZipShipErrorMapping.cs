namespace PBServices.Carriers.ZipShip;

/// <summary>Maps documented ZipShip error codes to our integration exception.</summary>
public static class ZipShipErrorMapping
{
    public static CarrierIntegrationException ToException(string body)
    {
        if (body.Contains("AUTH_INVALID_KEY"))
        {
            return new CarrierIntegrationException("AUTH_INVALID_KEY", "ZipShip API key is missing or invalid.");
        }

        if (body.Contains("VALIDATION_ERROR"))
        {
            return new CarrierIntegrationException("VALIDATION_ERROR", "ZipShip rejected the request as invalid.");
        }

        if (body.Contains("RATE_LIMITED"))
        {
            return new CarrierIntegrationException("RATE_LIMITED", "ZipShip rate limit exceeded; retry with backoff.");
        }

        if (body.Contains("SERVICE_UNAVAILABLE"))
        {
            return new CarrierIntegrationException("SERVICE_UNAVAILABLE", "ZipShip is temporarily unavailable.");
        }

        return new CarrierIntegrationException("UNKNOWN", $"Unmapped ZipShip error: {body}");
    }
}
