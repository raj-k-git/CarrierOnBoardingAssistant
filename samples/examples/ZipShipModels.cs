using System.Text.Json.Serialization;

namespace PBServices.Carriers.ZipShip;

public sealed class ZipShipLabelRequest
{
    [JsonPropertyName("service_code")]
    public string ServiceCode { get; set; } = string.Empty;

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("ship_from")]
    public ZipShipAddress ShipFrom { get; set; } = new();

    [JsonPropertyName("ship_to")]
    public ZipShipAddress ShipTo { get; set; } = new();

    [JsonPropertyName("parcel")]
    public ZipShipParcel Parcel { get; set; } = new();

    [JsonPropertyName("label_format")]
    public string LabelFormat { get; set; } = "PDF";
}

public sealed class ZipShipAddress
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("street1")]
    public string Street1 { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("postal_code")]
    public string PostalCode { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;
}

public sealed class ZipShipParcel
{
    [JsonPropertyName("weight")]
    public decimal Weight { get; set; }

    [JsonPropertyName("weight_unit")]
    public string WeightUnit { get; set; } = "LB";
}

public sealed class ZipShipLabelResponse
{
    [JsonPropertyName("label_id")]
    public string LabelId { get; set; } = string.Empty;

    [JsonPropertyName("tracking_number")]
    public string TrackingNumber { get; set; } = string.Empty;

    [JsonPropertyName("label_data")]
    public string LabelData { get; set; } = string.Empty;

    [JsonPropertyName("label_format")]
    public string LabelFormat { get; set; } = "PDF";
}
