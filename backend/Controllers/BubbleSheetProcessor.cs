using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Graduation_proj.Models;
using Microsoft.EntityFrameworkCore;

namespace Graduation_proj.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BubbleSheetProcessor : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BubbleSheetProcessor> _logger;
        private readonly string _uploadsFolder;

        public BubbleSheetProcessor(ApplicationDbContext context, IWebHostEnvironment hostEnvironment, ILogger<BubbleSheetProcessor> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _uploadsFolder = Path.Combine(hostEnvironment.WebRootPath, "Uploads");
            Directory.CreateDirectory(_uploadsFolder);
        }

        [HttpPost("CorrectBubbleSheets")]
        public async Task<IActionResult> CorrectBubbleSheets([FromForm] CorrectBubbleSheetsDto dto)
        {
            try
            {
                // Validate input
                if (dto.BubbleSheetFiles == null || !dto.BubbleSheetFiles.Any())
                {
                    _logger.LogWarning("No bubble sheet files uploaded for correction.");
                    return BadRequest(new { success = false, message = "No bubble sheet files uploaded." });
                }
                if (dto.ExcelFile == null)
                {
                    _logger.LogWarning("No Excel file uploaded for correction.");
                    return BadRequest(new { success = false, message = "No Excel file uploaded." });
                }

                // Save Excel file to temporary directory
                var excelFilePath = Path.Combine(_uploadsFolder, "Temp", $"exam_details_{Guid.NewGuid()}.xlsx");
                Directory.CreateDirectory(Path.GetDirectoryName(excelFilePath));
                using (var stream = new FileStream(excelFilePath, FileMode.Create))
                {
                    await dto.ExcelFile.CopyToAsync(stream);
                }

                // Save bubble sheet files to temporary directory
                var tempDir = Path.Combine(_uploadsFolder, "Temp", Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);
                var inputPath = tempDir;
                if (dto.BubbleSheetFiles.Count == 1)
                {
                    var file = dto.BubbleSheetFiles.First();
                    var filePath = Path.Combine(tempDir, file.FileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    inputPath = filePath;
                }
                else
                {
                    foreach (var file in dto.BubbleSheetFiles)
                    {
                        var filePath = Path.Combine(tempDir, file.FileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                    }
                }

                // Prepare output path for grades.xlsx
                var outputPath = Path.Combine(_uploadsFolder, "Temp", $"grades_{Guid.NewGuid()}.xlsx");

                // Call Correct.py
                string pythonPath = @"C:\Users\Eng.Ahmed\AppData\Local\Programs\Python\Python312\python.exe"; // Replace with your Python path
                string scriptPath = @"D:\Eng\Graduation_proj\python\Correct.py"; // Replace with your Correct.py path

                var arguments = new[]
                {
                       $"\"{scriptPath}\"",
                       "--input", $"\"{inputPath}\"",
                       "--excel", $"\"{excelFilePath}\"",
                       "--output", $"\"{outputPath}\""
                   };

                ProcessStartInfo start = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = string.Join(" ", arguments),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(start))
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0 || !string.IsNullOrEmpty(error))
                    {
                        _logger.LogError("Python script error: {Error}", error);
                        return StatusCode(500, new { success = false, message = $"Failed to correct bubble sheets: {error}" });
                    }
                    _logger.LogInformation("Python script output: {Output}", output);
                }

                // Read and return the output Excel file
                if (!System.IO.File.Exists(outputPath))
                {
                    _logger.LogError("Output Excel file not found: {OutputPath}", outputPath);
                    return StatusCode(500, new { success = false, message = "Failed to generate output file." });
                }

                var outputBytes = await System.IO.File.ReadAllBytesAsync(outputPath);
                _logger.LogInformation("Successfully corrected bubble sheets.");

                // Clean up temporary files
                try
                {
                    Directory.Delete(Path.GetDirectoryName(excelFilePath), true);
                    Directory.Delete(tempDir, true);
                    System.IO.File.Delete(outputPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to clean up temporary files: {Error}", ex.Message);
                }

                return File(outputBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "grades.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error correcting bubble sheets.");
                return StatusCode(500, new { success = false, message = $"Error correcting bubble sheets: {ex.Message}" });
            }
        }

        private IActionResult HandleException(Exception ex, string operation)
        {
            string errorDetails = ex.InnerException != null ? $" InnerException: {ex.InnerException.Message}" : "";
            if (ex is DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while {Operation}.{Details} StackTrace: {StackTrace}", operation, errorDetails, dbEx.StackTrace);
                return StatusCode(500, new { success = false, message = $"Database error while {operation}.", error = dbEx.InnerException?.Message ?? dbEx.Message });
            }

            _logger.LogError(ex, "Unexpected error while {Operation}.{Details} StackTrace: {StackTrace}", operation, errorDetails, ex.StackTrace);
            return StatusCode(500, new { success = false, message = $"Unexpected error while {operation}.", error = ex.Message + errorDetails });
        }
    }

    public class CorrectBubbleSheetsDto
    {
        public List<IFormFile> BubbleSheetFiles { get; set; }
        public IFormFile ExcelFile { get; set; }
    }
}