# Overview
This is a .NET Core Worker Service that generates an intra-day power trading report for traders. The report outputs aggregated volume per hour in local (Europe/London) time to a CSV file. The data is retrieved from a provided trading system (PowerService.dll) and the report runs on a configurable interval.
  
## Features
Fetches power trading data for the current date.    
Aggregates trading volume per local hour (starting from 23:00 of the previous day).    
Outputs a CSV report with:
Local Time (HH:mm)
Volume   
Configurable output folder and extract interval (via config file or command-line).  
Runs once immediately on startup, then at regular intervals. 
Structured logging using Serilog.  

## Technologies Used
.NET 8 Worker Service (dotnet new worker)  
C#  
PowerService.dll (provided trading system assembly)  
CsvHelper  
Serilog

## Configuration  
The application reads its settings from appsettings.json, with the option to override via command-line arguments.  
### appsettings.json
``` 
{
  "CsvFolderPath": "output",
  "ExtractInterval": "10"
}
```
### Command-Line  
```
dotnet run --CsvFolderPath="custom_folder" --ExtractInterval="5"
```
## CSV File generation  
**Filename format**: PowerPosition_YYYYMMDD_HHMM.csv  
**Example**: PowerPosition_20250506_0932.csv  
### Time Zone Handling (Europe/London – UK Local Time)  
- All date/time calculations in the application are based on UK local time (Europe/London time zone).  
- This includes:  
  - Fetching trade data for the current local day.  
  - Mapping trade periods to wall clock hours (starting from 23:00 of the previous day).  
  - Generating the CSV filename timestamp using local time.

## Build and Run 

#### 1. Clone the repository:    

To start, clone the repository to the local machine using the following command:   

```   

git clone  https://github.com/Sunita76/PowerPosition.git 

``` 
#### 2. Place PowerService.dll in the root or relevant directory.  
Add the reference to PowerService.dll in your project.

#### 3. Build the Project  

#### 4. Run 
