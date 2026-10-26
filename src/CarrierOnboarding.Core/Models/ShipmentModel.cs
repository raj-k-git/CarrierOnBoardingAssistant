namespace CarrierOnboarding.Core.Models;

/// <summary>
/// Our canonical, carrier-agnostic shipment model. The <c>generate</c> command maps
/// these source fields onto carrier-specific request fields. Keep this list in sync
/// with the real PBServices shipment contract when wiring up a production integration.
/// </summary>
public static class ShipmentModel
{
    /// <summary>Source field names available on our internal shipment request.</summary>
    public static readonly IReadOnlyList<string> Fields = new[]
    {
        "Shipment.CarrierServiceCode",
        "Shipment.ReferenceNumber",
        "Shipment.Sender.Name",
        "Shipment.Sender.AddressLine1",
        "Shipment.Sender.City",
        "Shipment.Sender.StateProvince",
        "Shipment.Sender.PostalCode",
        "Shipment.Sender.CountryCode",
        "Shipment.Sender.Phone",
        "Shipment.Recipient.Name",
        "Shipment.Recipient.AddressLine1",
        "Shipment.Recipient.City",
        "Shipment.Recipient.StateProvince",
        "Shipment.Recipient.PostalCode",
        "Shipment.Recipient.CountryCode",
        "Shipment.Recipient.Phone",
        "Shipment.Parcel.WeightValue",
        "Shipment.Parcel.WeightUnit",
        "Shipment.Parcel.LengthValue",
        "Shipment.Parcel.WidthValue",
        "Shipment.Parcel.HeightValue",
        "Shipment.Parcel.DimensionUnit",
        "Shipment.Parcel.DeclaredValue",
        "Shipment.Parcel.CurrencyCode",
        "Shipment.LabelFormat",
        "Shipment.LabelSize",
    };
}
