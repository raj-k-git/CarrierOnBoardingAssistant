# AcmeShip API Integration Guide (Sample)

> This is a small, fictional carrier API guide included so you can try the tool end to end.

## 1. Overview

AcmeShip provides a REST API to create shipping labels and track parcels.
All requests use JSON over HTTPS.

## 2. Environments

| Environment | Base URL |
|-------------|----------|
| Sandbox     | https://sandbox.api.acmeship.com/v1 |
| Production  | https://api.acmeship.com/v1 |

Use the Sandbox environment for all testing. Sandbox labels are watermarked and non-billable.

## 3. Authentication

AcmeShip uses API key authentication. Pass your key in the `Authorization` header
using the `ApiKey` scheme:

```
Authorization: ApiKey {your_api_key}
```

Keys are issued per environment. A Sandbox key will not work in Production.
Requests without a valid key return `401 AUTH_INVALID_KEY`.

## 4. Create Label

`POST /labels`

### Request fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `service_code` | string | yes | Shipping service, e.g. `GROUND`, `EXPRESS` |
| `reference` | string | no | Your shipment reference |
| `ship_from.name` | string | yes | Sender name |
| `ship_from.street1` | string | yes | Sender street address |
| `ship_from.city` | string | yes | Sender city |
| `ship_from.state` | string | yes | Sender state/province |
| `ship_from.postal_code` | string | yes | Sender postal code |
| `ship_from.country` | string | yes | Sender ISO country code |
| `ship_to.name` | string | yes | Recipient name |
| `ship_to.street1` | string | yes | Recipient street address |
| `ship_to.city` | string | yes | Recipient city |
| `ship_to.state` | string | yes | Recipient state/province |
| `ship_to.postal_code` | string | yes | Recipient postal code |
| `ship_to.country` | string | yes | Recipient ISO country code |
| `parcel.weight` | number | yes | Weight value |
| `parcel.weight_unit` | string | yes | `LB` or `KG` |
| `parcel.length` | number | no | Length |
| `parcel.width` | number | no | Width |
| `parcel.height` | number | no | Height |
| `parcel.dimension_unit` | string | no | `IN` or `CM` |
| `label_format` | string | no | `PDF` (default) or `ZPL` |

### Response fields

| Field | Type | Description |
|-------|------|-------------|
| `label_id` | string | AcmeShip label identifier |
| `tracking_number` | string | Carrier tracking number |
| `label_data` | string | Base64-encoded label document |
| `label_format` | string | Format of `label_data` |

## 5. Error Codes

| Code | HTTP | Meaning |
|------|------|---------|
| `AUTH_INVALID_KEY` | 401 | API key missing or invalid |
| `VALIDATION_ERROR` | 400 | A required field is missing or invalid |
| `SERVICE_UNAVAILABLE` | 503 | Carrier system temporarily unavailable |
| `RATE_LIMITED` | 429 | Too many requests |

## 6. Rate Limits

The API allows up to 50 requests per second per API key. Exceeding this limit
returns `429 RATE_LIMITED`. Clients should retry with exponential backoff.
