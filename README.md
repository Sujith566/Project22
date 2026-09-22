# Laboratory Order Intake and Input Validation Service

A C# service that accepts a laboratory order as a JSON string, validates the input, and returns either an accepted typed order or a rejected result containing validation errors.

## Technologies

- C#
- .NET
- System.Text.Json
- xUnit
- Git / GitHub

## Project Structure

```text
Project22/
├── src/
│   └── OrderIntake/
│       ├── Order.cs
│       ├── OrderIntake.csproj
│       ├── OrderIntakeService.cs
│       ├── OrderResults.cs
│       └── ValidationError.cs
│
├── tests/
│   └── OrderIntake.Tests/
│       ├── OrderIntake.Tests.csproj
│       └── UnitTest1.cs
│
├── .gitignore
├── Project22.slnx
└── README.md