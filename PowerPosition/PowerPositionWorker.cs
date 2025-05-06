using CsvHelper;
using System.Globalization;
using Axpo;
namespace PowerPosition
{
    public class PowerPositionWorker : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly string _folderPath;
        private readonly ILogger<PowerPositionWorker> _logger; 
        private readonly short _extractInterval;
        
        public PowerPositionWorker(IConfiguration configuration, ILogger<PowerPositionWorker> logger)
        {
            _configuration = configuration;
            var configuredPath = _configuration["CsvFolderPath"] ?? "data";
            _folderPath = Path.GetFullPath(configuredPath, Directory.GetCurrentDirectory());
            Directory.CreateDirectory(_folderPath);
            _extractInterval = short.TryParse(_configuration["ExtractInterval"], out var interval) ? interval : (short)10;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // First immediate run
            try
            {
                WritePowerPositionToCsv(_folderPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during initial execution of WritePowerPositionToCSV.");
            }

            // Run at interval
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(_extractInterval), stoppingToken);
                    WritePowerPositionToCsv(_folderPath);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("PowerPositionWorker cancellation requested.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during scheduled execution of WritePowerPositionToCSV.");
                }
            }

        }
        private static string[] GetPeriodToTimeMap(DateTime localDateTime)
        {
            string[] periodToTimeMap = new string[24];
            DateTime periodStart = new DateTime(localDateTime.Year, localDateTime.Month, localDateTime.Day, 23, 0, 0);
            periodStart = periodStart.AddDays(-1); // Because 23:00 of previous day
            return Enumerable.Range(0, 24)
                     .Select(i => periodStart.AddHours(i).ToString("HH:mm"))
                     .ToArray();
        }
        
        public void WritePowerPositionToCsv(string folderPath)
        {
               //Local time is in the Europe/London (Dublin, Edinburgh, Lisbon, London in Microsoft Windows) time zone. 
                TimeZoneInfo localTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"); 
                DateTime localDateNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, localTimeZone);
          
                string[] periodToTimeMap = GetPeriodToTimeMap(localDateNow); //Mapping period to time
                string csvFileName = $"PowerPosition_{localDateNow:yyyyMMdd_HHmm}.csv";
                string filePath = Path.Combine(folderPath, csvFileName);
                try
                {
                    _logger.LogInformation("Starting power position CSV generation : {FileName}", csvFileName);
                    PowerService powerService = new PowerService();
                    IEnumerable<PowerTrade> powerTrade = powerService.GetTrades(localDateNow.Date);
                    
                    var powerTradeValues = powerTrade.SelectMany(
                        powerTrade => powerTrade.Periods,
                        (trade, period) => new { period.Period, period.Volume }).OrderBy(a => a.Period); ;

                    var aggregatedTrades = powerTradeValues
                        .GroupBy(powerTrade => powerTrade.Period)
                        .Select(g => new 
                        {
                            Period = g.Key,
                            PeriodInHrs = (g.Key >= 1 && g.Key <= 24)
                                   ? periodToTimeMap[g.Key - 1]
                                   : "Unknown Time",
                            VolumeTotal = Math.Round(g.Sum(pT => pT.Volume), 3),
                           
                        })
                        .OrderBy(a => a.Period);
                    var csvRecords = aggregatedTrades.Select(p => new
                    {
                        LocalTime = p.PeriodInHrs,
                        Volume = p.VolumeTotal
                    });
  
                    using var writer = new StreamWriter(filePath);
                    using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csvWriter.WriteRecords(csvRecords);
                    }
                    _logger.LogInformation("Power position file written successfully: {FileName}", csvFileName);

                }
                catch (Exception ex)

                {
                    _logger.LogError(ex, "An error occurred while writing the power position CSV file at {FileName}", csvFileName);
                }
        }
    }
   
   
}
