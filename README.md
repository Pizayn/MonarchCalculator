# Monarch Calculator

This project fetches a list of English monarchs from a remote data source and processes it to answer the following questions:

1. How many monarchs are there in the list?
2. Which monarch ruled the longest (and for how long)?
3. Which house ruled the longest (and for how long)?
4. What was the most common first name?

The code follows a clean, maintainable style using C#, .NET, and modern development practices (e.g., Dependency Injection, caching, testing, and logging). 

---

## Table of Contents

- [Overview](#overview)
- [Technologies](#technologies)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
- [Usage](#usage)
- [Testing](#testing)
- [Configuration](#configuration)
- [Logging](#logging)
- [License](#license)

---

## Overview

The **Monarch Calculator** is a console application that:
1. Fetches monarch data from a specified URL (JSON format).
2. Deserializes it into C# objects.
3. Provides statistics such as:
   - The total count of monarchs.
   - The longest-ruling monarch and duration of rule.
   - The longest-ruling royal house and duration.
   - The most common first name among monarchs.

Data is cached in memory for a configurable duration to optimize repeated calls.

---

## Technologies

- **.NET CORE 3.1  - Console application.
- **Newtonsoft.Json** - For JSON deserialization.
- **xUnit** - For unit testing.
- **Moq** - For mocking dependencies in tests.

---

## Project Structure

```plaintext
MonarchCalculator/
  ├─ Program.cs                  
  
MonarchsCalculator.Tests/
  ├─ MonarchServiceTests.cs
  └─ MonarchExtensionsTests.cs
```

---

## Getting Started

### Prerequisites

- [.NET 3.1 SDK or higher](https://dotnet.microsoft.com/download)
- An Internet connection (to fetch the remote JSON data).
- (Optional) [Visual Studio](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/) for development.

### Installation

1. **Clone** or **download** this repository.
2. Open the solution in your preferred IDE (Visual Studio/VS Code), or navigate to the project folder via a terminal.

---

## Usage


1. **Run the Console App**:
   - **From command line**:
     ```bash
     cd MonarchCalculator
     dotnet run --project MonarchCalculator.csproj
     ```
   - **From Visual Studio/VS Code**:
     - Open the solution and press `F5` or select **Debug -> Start Debugging**.

2. **View the Output**:  
   The console will print:
   - Total monarch count  
   - Longest ruling monarch and years  
   - Longest ruling house and total years  
   - Most common first name  

Example console output might look like this:
```plaintext
[INFO] Application is starting...
[INFO] Fetching data from remote...
[INFO] 1) Total monarch count   : 57
[INFO] 2) Longest ruling monarch: Elizabeth II (73 years)
[INFO] 3) Longest ruling house  : House of Windsor (187 years)
[INFO] 4) Most common first name: Edward
[INFO] Application completed successfully.
```

---

## Testing

Unit tests are written using **xUnit**. They cover:
- Caching logic (ensuring data is retrieved from cache if available).
- Repository fetch logic (including error scenarios).
- Service methods (finding the longest ruling monarch/house, most common first name, etc.).

To run the tests:

1. Ensure you are in the solution root directory.
2. Run:
   ```bash
   dotnet test
   ```
3. Test results will appear in the console.

---

## Configuration

Key configuration settings are located in `AppSettings.cs`:

- `DataUrl`: The URL for fetching monarch data (JSON).
- `HttpTimeout`: The timeout in seconds for the HTTP request (default 30s).
- `CacheDurationMinutes`: Duration (in minutes) to store data in memory cache (default 5).
- `ParallelThreshold`: Threshold above which parallel LINQ queries will be used (default 10,000).

---

## Logging

A basic `Logger` class is provided:
- `LogInfo(string msg)`
- `LogWarning(string msg)`
- `LogError(string msg, Exception ex = null)`

All logs are written to the console with color-coded output (green for info, yellow for warnings, red for errors).

---



## License

This project is provided for demonstration purposes. No specific license is attached. Please adapt and reuse freely in your own or organizational projects.

---

**Enjoy using the Monarch Calculator!** If you have any questions or suggestions, feel free to open an issue or contribute a pull request. Happy coding!
