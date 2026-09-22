using OrderIntake;

namespace OrderIntake.Tests;

public class OrderIntakeServiceTests
{
    [Fact]
    public void ValidOrder_IsAccepted_AndValuesAreNormalized()
    {
        var service = new OrderIntakeService();

        string json = """
        {
            "orderId": "ORD-1005",
            "patientId": "PAT-505",
            "specimenId": "SP-9005",
            "specimenType": "bLoOd",
            "priority": "URGENT",
            "collectionDate": "2026-09-18",
            "requestedTests": ["Glucose", "CompleteBloodCount"],
            "senderNote": "ignore me"
        }
        """;

        OrderResult result = service.Process(json);

        Assert.Equal("Accepted", result.Status);
        Assert.NotNull(result.Order);
        Assert.Empty(result.Errors);

        Assert.Equal("ORD-1005", result.Order!.OrderId);
        Assert.Equal("PAT-505", result.Order.PatientId);
        Assert.Equal("SP-9005", result.Order.SpecimenId);

        Assert.Equal("Blood", result.Order.SpecimenType);
        Assert.Equal("Urgent", result.Order.Priority);

        Assert.Equal(
            new DateTime(2026, 9, 18),
            result.Order.CollectionDate);

        Assert.Equal(
            new[] { "Glucose", "CompleteBloodCount" },
            result.Order.RequestedTests);
    }
    [Fact]
public void InvalidOrder_ReturnsAllValidationErrors()
{
    var service = new OrderIntakeService();

    string json = """
    {
        "orderId": "   ",
        "patientId": "PAT-505",
        "specimenId": "SP-9005",
        "specimenType": "Hair",
        "priority": "Critical",
        "collectionDate": "2026/09/18",
        "requestedTests": []
    }
    """;

    OrderResult result = service.Process(json);

    Assert.Equal("Rejected", result.Status);
    Assert.Null(result.Order);

    Assert.Equal(5, result.Errors.Count);

    Assert.Contains(result.Errors,
        error => error.Field == "orderId" &&
                 error.Code == "REQUIRED");

    Assert.Contains(result.Errors,
        error => error.Field == "specimenType" &&
                 error.Code == "INVALID_VALUE");

    Assert.Contains(result.Errors,
        error => error.Field == "priority" &&
                 error.Code == "INVALID_VALUE");

    Assert.Contains(result.Errors,
        error => error.Field == "collectionDate" &&
                 error.Code == "INVALID_FORMAT");

    Assert.Contains(result.Errors,
        error => error.Field == "requestedTests" &&
                 error.Code == "REQUIRED");
}
[Fact]
public void OrderId_Exactly20Characters_IsAccepted()
{
    var service = new OrderIntakeService();

    string orderId = new string('A', 20);

    string json = $$"""
    {
        "orderId": "{{orderId}}",
        "patientId": "PAT-505",
        "specimenId": "SP-9005",
        "specimenType": "Blood",
        "priority": "Routine",
        "collectionDate": "2026-09-18",
        "requestedTests": ["Glucose"]
    }
    """;

    OrderResult result = service.Process(json);

    Assert.Equal("Accepted", result.Status);
    Assert.NotNull(result.Order);
    Assert.Equal(20, result.Order!.OrderId.Length);
    Assert.Empty(result.Errors);
}
[Fact]
public void OrderId_21Characters_IsRejected()
{
    var service = new OrderIntakeService();

    string orderId = new string('A', 21);

    string json = $$"""
    {
        "orderId": "{{orderId}}",
        "patientId": "PAT-505",
        "specimenId": "SP-9005",
        "specimenType": "Blood",
        "priority": "Routine",
        "collectionDate": "2026-09-18",
        "requestedTests": ["Glucose"]
    }
    """;

    OrderResult result = service.Process(json);

    Assert.Equal("Rejected", result.Status);
    Assert.Null(result.Order);

    Assert.Contains(result.Errors,
        error => error.Field == "orderId" &&
                 error.Code == "MAX_LENGTH");
}
[Fact]
public void InvalidCalendarDate_IsRejected()
{
    var service = new OrderIntakeService();

    string json = """
    {
        "orderId": "ORD-1005",
        "patientId": "PAT-505",
        "specimenId": "SP-9005",
        "specimenType": "Blood",
        "priority": "Routine",
        "collectionDate": "2026-02-30",
        "requestedTests": ["Glucose"]
    }
    """;

    OrderResult result = service.Process(json);

    Assert.Equal("Rejected", result.Status);
    Assert.Null(result.Order);

    Assert.Contains(result.Errors,
        error => error.Field == "collectionDate" &&
                 error.Code == "INVALID_FORMAT");
}
[Fact]
public void InvalidDateFormat_IsRejected()
{
    var service = new OrderIntakeService();

    string json = """
    {
        "orderId": "ORD-1005",
        "patientId": "PAT-505",
        "specimenId": "SP-9005",
        "specimenType": "Blood",
        "priority": "Routine",
        "collectionDate": "20-09-2026",
        "requestedTests": ["Glucose"]
    }
    """;

    OrderResult result = service.Process(json);

    Assert.Equal("Rejected", result.Status);
    Assert.Null(result.Order);

    Assert.Contains(result.Errors,
        error => error.Field == "collectionDate" &&
                 error.Code == "INVALID_FORMAT");
}
[Fact]
public void FutureCollectionDate_IsRejected()
{
    var service = new OrderIntakeService();

    string futureDate = DateTime.Today
        .AddDays(1)
        .ToString("yyyy-MM-dd");

    string json = $$"""
    {
        "orderId": "ORD-1005",
        "patientId": "PAT-505",
        "specimenId": "SP-9005",
        "specimenType": "Blood",
        "priority": "Routine",
        "collectionDate": "{{futureDate}}",
        "requestedTests": ["Glucose"]
    }
    """;

    OrderResult result = service.Process(json);

    Assert.Equal("Rejected", result.Status);
    Assert.Null(result.Order);

    Assert.Contains(result.Errors,
        error => error.Field == "collectionDate" &&
                 error.Code == "FUTURE_DATE");
}
[Fact]
public void EmptyRequestedTests_IsRejected()
{
    var service = new OrderIntakeService();

    string json = """
    {
        "orderId": "ORD-1005",
        "patientId": "PAT-505",
        "specimenId": "SP-9005",
        "specimenType": "Blood",
        "priority": "Routine",
        "collectionDate": "2026-09-18",
        "requestedTests": []
    }
    """;

    OrderResult result = service.Process(json);

    Assert.Equal("Rejected", result.Status);
    Assert.Null(result.Order);

    Assert.Contains(result.Errors,
        error => error.Field == "requestedTests" &&
                 error.Code == "REQUIRED");
}
[Fact]
public void DuplicateRequestedTests_IgnoringCase_IsRejected()
{
    var service = new OrderIntakeService();

    string json = """
    {
        "orderId": "ORD-1005",
        "patientId": "PAT-505",
        "specimenId": "SP-9005",
        "specimenType": "Blood",
        "priority": "Routine",
        "collectionDate": "2026-09-18",
        "requestedTests": ["Glucose", "glucose"]
    }
    """;

    OrderResult result = service.Process(json);

    Assert.Equal("Rejected", result.Status);
    Assert.Null(result.Order);

    Assert.Contains(result.Errors,
        error => error.Field == "requestedTests" &&
                 error.Code == "DUPLICATE");
}
[Fact]
public void BrokenJson_ReturnsSingleMalformedInputError()
{
    var service = new OrderIntakeService();

    string json = """
    {
        "orderId": "ORD-1005",
        "patientId": "PAT-505",
        "specimenId": "SP-9005",
        "specimenType": "Blood",
        "priority": "Routine",
        "collectionDate": "2026-09-18",
        "requestedTests": ["Glucose"
    """;

    OrderResult result = service.Process(json);

    Assert.Equal("Rejected", result.Status);
    Assert.Null(result.Order);

    Assert.Single(result.Errors);

    Assert.Equal("$", result.Errors[0].Field);
    Assert.Equal("MALFORMED_INPUT", result.Errors[0].Code);
}
[Fact]
public void EmptyObject_ReturnsFieldValidationErrors()
{
    var service = new OrderIntakeService();

    OrderResult result = service.Process("{}");

    Assert.Equal("Rejected", result.Status);
    Assert.Null(result.Order);
    Assert.DoesNotContain(result.Errors,
        error => error.Code == "MALFORMED_INPUT");

    Assert.Contains(result.Errors,
        error => error.Field == "orderId" &&
                 error.Code == "REQUIRED");

    Assert.Contains(result.Errors,
        error => error.Field == "patientId" &&
                 error.Code == "REQUIRED");

    Assert.Contains(result.Errors,
        error => error.Field == "specimenId" &&
                 error.Code == "REQUIRED");
}
[Fact]
public void NullRequiredField_ReturnsRequiredError()
{
    var service = new OrderIntakeService();

    string json = """
    {
        "orderId": null,
        "patientId": "PAT-505",
        "specimenId": "SP-9005",
        "specimenType": "Blood",
        "priority": "Routine",
        "collectionDate": "2026-09-18",
        "requestedTests": ["Glucose"]
    }
    """;

    OrderResult result = service.Process(json);

    Assert.Equal("Rejected", result.Status);
    Assert.Null(result.Order);

    Assert.Contains(result.Errors,
        error => error.Field == "orderId" &&
                 error.Code == "REQUIRED");
}
[Fact]
public void TopLevelArray_ReturnsMalformedInput()
{
    var service = new OrderIntakeService();

    OrderResult result = service.Process("[]");

    Assert.Equal("Rejected", result.Status);
    Assert.Null(result.Order);
    Assert.Single(result.Errors);

    Assert.Equal("$", result.Errors[0].Field);
    Assert.Equal("MALFORMED_INPUT", result.Errors[0].Code);
}
[Fact]
public void WrongRecognizedFieldType_ReturnsMalformedInput()
{
    var service = new OrderIntakeService();

    string json = """
    {
        "orderId": 123,
        "patientId": "PAT-505",
        "specimenId": "SP-9005",
        "specimenType": "Blood",
        "priority": "Routine",
        "collectionDate": "2026-09-18",
        "requestedTests": ["Glucose"]
    }
    """;

    OrderResult result = service.Process(json);

    Assert.Equal("Rejected", result.Status);
    Assert.Null(result.Order);
    Assert.Single(result.Errors);

    Assert.Equal("$", result.Errors[0].Field);
    Assert.Equal("MALFORMED_INPUT", result.Errors[0].Code);
}
[Fact]
public void TodaysCollectionDate_IsAccepted()
{
    var service = new OrderIntakeService();

    string today = DateTime.Today.ToString("yyyy-MM-dd");

    string json = $$"""
    {
        "orderId": "ORD-1005",
        "patientId": "PAT-505",
        "specimenId": "SP-9005",
        "specimenType": "Blood",
        "priority": "Routine",
        "collectionDate": "{{today}}",
        "requestedTests": ["Glucose"]
    }
    """;

    OrderResult result = service.Process(json);

    Assert.Equal("Accepted", result.Status);
    Assert.NotNull(result.Order);
    Assert.Equal(DateTime.Today, result.Order!.CollectionDate);
    Assert.Empty(result.Errors);
}
[Fact]
public void MalformedJson_ReturnsMalformedInputError()
{
    var service = new OrderIntakeService();

    string json = """
    {
        "orderId": "ORD-1005",
        "patientId": "PAT-505",
        "specimenId": "SP-9005",
        "specimenType": "Blood",
        "priority": "Routine",
        "collectionDate": "2026-09-18",
        "requestedTests": ["Glucose"
    }
    """;

    OrderResult result = service.Process(json);

    Assert.Equal("Rejected", result.Status);
    Assert.Null(result.Order);
    Assert.Single(result.Errors);

    Assert.Equal("$", result.Errors[0].Field);
    Assert.Equal("MALFORMED_INPUT", result.Errors[0].Code);
}
}