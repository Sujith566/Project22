using System.Globalization;
using System.Text.Json;

namespace OrderIntake;

public class OrderIntakeService
{
    public OrderResult Process(string json)
    {
        var result = new OrderResult();

        if (string.IsNullOrWhiteSpace(json))
        {
            result.Status = "Rejected";
            result.Errors.Add(new ValidationError
            {
                Field = "$",
                Code = "MALFORMED_INPUT",
                Message = "Input is empty."
            });

            return result;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                result.Status = "Rejected";
                result.Errors.Add(new ValidationError
                {
                    Field = "$",
                    Code = "MALFORMED_INPUT",
                    Message = "Input must be a JSON object."
                });

                return result;
            }

            JsonElement root = document.RootElement;

            string? orderId = null;
            string? patientId = null;
            string? specimenId = null;
            string? specimenType = null;
            string? priority = null;
            string? collectionDateText = null;
            List<string>? requestedTests = null;

            bool validTypes =
                TryReadString(root, "orderId", out orderId) &&
                TryReadString(root, "patientId", out patientId) &&
                TryReadString(root, "specimenId", out specimenId) &&
                TryReadString(root, "specimenType", out specimenType) &&
                TryReadString(root, "priority", out priority) &&
                TryReadString(root, "collectionDate", out collectionDateText) &&
                TryReadRequestedTests(root, "requestedTests", out requestedTests);

            if (!validTypes)
            {
                result.Status = "Rejected";
                result.Errors.Add(new ValidationError
                {
                    Field = "$",
                    Code = "MALFORMED_INPUT",
                    Message = "One or more fields have an incompatible JSON type."
                });

                return result;
            }

            ValidateId(orderId, "orderId", result.Errors);
            ValidateId(patientId, "patientId", result.Errors);
            ValidateId(specimenId, "specimenId", result.Errors);
            string? normalizedSpecimenType = ValidateChoice(
                specimenType,
                "specimenType",
                new[] { "Blood", "Urine", "Tissue", "Saliva" },
                result.Errors);

            string? normalizedPriority = ValidateChoice(
                priority,
                "priority",
                new[] { "Routine", "Urgent" },
                result.Errors);
            DateTime? collectionDate = ValidateCollectionDate(
                    collectionDateText,
                    result.Errors);
            ValidateRequestedTests(requestedTests, result.Errors);

            if (result.Errors.Count > 0)
            {
                result.Status = "Rejected";
                return result;
            }

            result.Order = new Order
            {
                OrderId = orderId!,
                PatientId = patientId!,
                SpecimenId = specimenId!,
                SpecimenType = normalizedSpecimenType!,
                Priority = normalizedPriority!,
                CollectionDate = collectionDate!.Value,
                RequestedTests = requestedTests!
            };

            result.Status = "Accepted";

            return result;
        }
        catch (JsonException)
        {
            result.Status = "Rejected";
            result.Errors.Add(new ValidationError
            {
                Field = "$",
                Code = "MALFORMED_INPUT",
                Message = "Input is not valid JSON."
            });

            return result;
        }
    }

    private bool TryReadString(
        JsonElement root,
        string propertyName,
        out string? value)
    {
        value = null;

        if (!root.TryGetProperty(propertyName, out JsonElement element))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = element.GetString();

        return true;
    }

    private bool TryReadRequestedTests(
        JsonElement root,
        string propertyName,
        out List<string>? tests)
    {
        tests = null;

        if (!root.TryGetProperty(propertyName, out JsonElement element))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        tests = new List<string>();

        foreach (JsonElement item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            tests.Add(item.GetString() ?? "");
        }

        return true;
    }
    private void ValidateId(
        string? value,
        string fieldName,
        List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError
            {
                Field = fieldName,
                Code = "REQUIRED",
                Message = $"{fieldName} is required."
            });

            return;
        }

        if (value.Length > 20)
        {
            errors.Add(new ValidationError
            {
                Field = fieldName,
                Code = "MAX_LENGTH",
                Message = $"{fieldName} must be 20 characters or fewer."
            });
        }
    }
    private string? ValidateChoice(
        string? value,
        string fieldName,
        string[] allowedValues,
        List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError
            {
                Field = fieldName,
                Code = "REQUIRED",
                Message = $"{fieldName} is required."
            });

            return null;
        }

        foreach (string allowedValue in allowedValues)
        {
            if (string.Equals(value, allowedValue, StringComparison.OrdinalIgnoreCase))
            {
                return allowedValue;
            }
        }

        errors.Add(new ValidationError
        {
            Field = fieldName,
            Code = "INVALID_VALUE",
            Message = $"{fieldName} has an invalid value."
        });

        return null;
    }
    private DateTime? ValidateCollectionDate(
        string? value,
        List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError
            {
                Field = "collectionDate",
                Code = "REQUIRED",
                Message = "collectionDate is required."
            });

            return null;
        }

        if (!DateTime.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime date))
        {
            errors.Add(new ValidationError
            {
                Field = "collectionDate",
                Code = "INVALID_FORMAT",
                Message = "collectionDate must be a valid date in yyyy-MM-dd format."
            });

            return null;
        }

        if (date.Date > DateTime.Today)
        {
            errors.Add(new ValidationError
            {
                Field = "collectionDate",
                Code = "FUTURE_DATE",
                Message = "collectionDate cannot be in the future."
            });

            return null;
        }

        return date.Date;
    }
    private void ValidateRequestedTests(
        List<string>? tests,
        List<ValidationError> errors)
    {
        if (tests == null || tests.Count == 0)
        {
            errors.Add(new ValidationError
            {
                Field = "requestedTests",
                Code = "REQUIRED",
                Message = "At least one requested test is required."
            });

            return;
        }

        bool hasEmptyItem = false;
        bool hasDuplicate = false;

        HashSet<string> seenTests = new(StringComparer.OrdinalIgnoreCase);

        foreach (string test in tests)
        {
            if (string.IsNullOrWhiteSpace(test))
            {
                hasEmptyItem = true;
                continue;
            }

            if (!seenTests.Add(test))
            {
                hasDuplicate = true;
            }
        }

        if (hasEmptyItem)
        {
            errors.Add(new ValidationError
            {
                Field = "requestedTests",
                Code = "INVALID_VALUE",
                Message = "Requested tests cannot contain empty items."
            });
        }

        if (hasDuplicate)
        {
            errors.Add(new ValidationError
            {
                Field = "requestedTests",
                Code = "DUPLICATE",
                Message = "Requested tests cannot contain duplicates."
            });
        }
    }
    }
