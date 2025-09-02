using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Graduation_proj.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Graduation_proj.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExamFilesGenerator : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExamFilesGenerator> _logger;
        private readonly string _uploadsFolder;

        public ExamFilesGenerator(ApplicationDbContext context, IWebHostEnvironment hostEnvironment, ILogger<ExamFilesGenerator> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _uploadsFolder = Path.Combine(hostEnvironment.WebRootPath, "Uploads");
            Directory.CreateDirectory(_uploadsFolder);
        }

        [HttpPost("GenerateExamFiles")]
        public IActionResult GenerateExamFiles([FromBody] GenerateExamFilesDto dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for GenerateExamFiles: {Errors}", string.Join("; ", GetModelStateErrors()));
                return BadRequest(new { success = false, message = string.Join("; ", GetModelStateErrors()) });
            }

            try
            {
                _logger.LogInformation("Starting file generation for ExamId {Id} with {NumberOfModels} models.", dto.Id, dto.NumberOfModels);

                var examUnits = _context.ExamUnits
                    .Include(e => e.Exam)
                    .ThenInclude(ex => ex.Material)
                    .Include(e => e.Group)
                    .ThenInclude(g => g.Problems)
                    .ThenInclude(p => p.ProblemChoices)
                    .Where(e => e.ExamId == dto.Id)
                    .AsNoTracking()
                    .ToList();

                if (!examUnits.Any())
                {
                    _logger.LogWarning("No exam units found for ExamId {Id}.", dto.Id);
                    return NotFound(new { success = false, message = "No exam units found for the specified exam." });
                }

                if (examUnits.Any(eu => eu.Exam == null || eu.Group == null || eu.Group.Problems == null || !eu.Group.Problems.Any()))
                {
                    _logger.LogWarning("Invalid data for ExamId {Id}: Missing Exam, Group, or Problems.", dto.Id);
                    return BadRequest(new { success = false, message = "Invalid exam data. Ensure exam, group, and problems are properly configured." });
                }

                var firstExam = examUnits.First().Exam;
                int totalQuestions = examUnits.Sum(eu => eu.TotalProblems);
                var bubbleSheetPaths = GenerateBubbleSheets(firstExam, dto.NumberOfModels, totalQuestions).Result;

                var examName = firstExam.ExamName?.Replace(".", "")?.Replace(" ", "_") ?? "Exam";
                var zipFileName = $"Exam_{examName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.zip";

                var wordFiles = new List<byte[]>();
                var excelFile = GenerateExcelFile(examUnits, dto.NumberOfModels);

                try
                {
                    for (int i = 0; i < dto.NumberOfModels; i++)
                    {
                        _logger.LogDebug("Generating Word file for model {ModelNumber} of ExamId {Id}.", i + 1, dto.Id);
                        var shuffledExamUnits = ShuffleExamUnits(examUnits);
                        var wordFile = GenerateWordFile(shuffledExamUnits, i + 1, bubbleSheetPaths[i], totalQuestions);
                        wordFiles.Add(wordFile);
                    }
                }
                finally
                {
                    // Clean up bubble sheet files
                    if (bubbleSheetPaths.Any())
                    {
                        var directoryPath = Path.GetDirectoryName(bubbleSheetPaths.First());
                        if (Directory.Exists(directoryPath))
                        {
                            try
                            {
                                Directory.Delete(directoryPath, true);
                                _logger.LogInformation("Deleted temporary bubble sheets directory: {DirectoryPath}", directoryPath);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to delete temporary bubble sheets directory: {DirectoryPath}", directoryPath);
                            }
                        }
                    }
                }

                _logger.LogDebug("Creating ZIP file for ExamId {Id}.", dto.Id);
                var zipFile = CreateZipFile(excelFile, wordFiles);

                if (zipFile.Length > 250 * 1024 * 1024)
                {
                    _logger.LogWarning("Generated ZIP file for ExamId {Id} is too large: {Size} bytes.", dto.Id, zipFile.Length);
                    return StatusCode(413, new { success = false, errorMessage = "Generated file is too large." });
                }

                _logger.LogInformation("Successfully generated ZIP file for ExamId {Id} with {NumberOfModels} models.", dto.Id, dto.NumberOfModels);
                return File(zipFile, "application/zip", zipFileName);
            }
            catch (Exception ex)
            {
                return HandleException(ex, $"generating files for ExamId {dto.Id}");
            }
        }

        private byte[] CreateZipFile(byte[] excelFile, List<byte[]> wordFiles)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    var excelEntry = archive.CreateEntry("exam_details.xlsx");
                    using (var entryStream = excelEntry.Open())
                    {
                        entryStream.Write(excelFile, 0, excelFile.Length);
                        entryStream.Flush();
                    }

                    for (int i = 0; i < wordFiles.Count; i++)
                    {
                        var wordEntry = archive.CreateEntry($"exam_version_{i + 1}.docx");
                        using (var entryStream = wordEntry.Open())
                        {
                            entryStream.Write(wordFiles[i], 0, wordFiles[i].Length);
                            entryStream.Flush();
                        }
                    }
                }

                memoryStream.Flush();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ZIP file. StackTrace: {StackTrace}", ex.StackTrace);
                throw new Exception("Failed to create ZIP file.", ex);
            }
        }

        private byte[] GenerateExcelFile(List<ExamUnit> examUnits, int numberOfModels)
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Exam Details");

                System.Drawing.Color[] modelColors =
                {
                    System.Drawing.Color.LightGreen,
                    System.Drawing.Color.LightCoral,
                    System.Drawing.Color.LightSkyBlue,
                    System.Drawing.Color.LightSalmon,
                    System.Drawing.Color.LightGoldenrodYellow
                };

                int totalColumns = numberOfModels * 3 - 1;

                if (examUnits.Any())
                {
                    var examName = examUnits.First()?.Exam?.ExamName ?? "Unknown Exam";
                    worksheet.Cells[1, 1, 1, totalColumns].Merge = true;
                    worksheet.Cells[1, 1].Value = $"Exam Name: {examName}";
                    worksheet.Cells[1, 1, 1, totalColumns].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, 1, 1, totalColumns].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Blue);
                    worksheet.Cells[1, 1, 1, totalColumns].Style.Font.Color.SetColor(System.Drawing.Color.White);
                    worksheet.Cells[1, 1, 1, totalColumns].Style.Font.Bold = true;
                    worksheet.Cells[1, 1, 1, totalColumns].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[1, 1, 1, totalColumns].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                int startColumn = 1;
                int rowStart = 3;

                for (int modelNumber = 1; modelNumber <= numberOfModels; modelNumber++)
                {
                    var shuffledExamUnits = ShuffleExamUnits(examUnits);
                    System.Drawing.Color modelColor = modelColors[(modelNumber - 1) % modelColors.Length];

                    int col1 = startColumn;
                    int col2 = startColumn + 1;
                    int row = rowStart;

                    worksheet.Cells[row, col1].Value = $"Model {modelNumber}";
                    worksheet.Cells[row, col1, row, col2].Merge = true;
                    worksheet.Cells[row, col1, row, col2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, col1, row, col2].Style.Fill.BackgroundColor.SetColor(modelColor);
                    worksheet.Cells[row, col1, row, col2].Style.Font.Bold = true;
                    worksheet.Cells[row, col1, row, col2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, col1, row, col2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    row++;

                    worksheet.Cells[row, col1].Value = "Questions";
                    worksheet.Cells[row, col2].Value = "Correct Answer";
                    worksheet.Cells[row, col1, row, col2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, col1, row, col2].Style.Fill.BackgroundColor.SetColor(modelColor);
                    worksheet.Cells[row, col1, row, col2].Style.Font.Bold = true;
                    worksheet.Cells[row, col1, row, col2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    row++;

                    int questionNumber = 1;
                    foreach (var examUnit in shuffledExamUnits)
                    {
                        if (examUnit?.Group?.Problems == null) continue;

                        foreach (var problem in examUnit.Group.Problems)
                        {
                            var choicesList = problem.ProblemChoices?.ToList() ?? new List<ProblemChoice>();
                            var correctChoice = choicesList.FirstOrDefault(c => c.UnitOrder == problem.RightAnswer);
                            worksheet.Cells[row, col1].Value = $"Question {questionNumber}";
                            worksheet.Cells[row, col2].Value = correctChoice != null ? choicesList.IndexOf(correctChoice) + 1 : "-";

                            worksheet.Cells[row, col1, row, col2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[row, col1, row, col2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                            worksheet.Cells[row, col1, row, col2].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                            row++;
                            questionNumber++;
                        }
                    }

                    startColumn += 3;
                }

                for (int col = 1; col <= totalColumns; col++)
                {
                    worksheet.Column(col).AutoFit();
                }

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("License"))
                {
                    _logger.LogError(ex, "EPPlus license error for ExamId {Id}. Please configure ExcelPackage.License. StackTrace: {StackTrace}", examUnits.FirstOrDefault()?.ExamId, ex.StackTrace);
                    throw new Exception("EPPlus license not configured. Please set ExcelPackage.License in Program.cs.");
                }
                _logger.LogError(ex, "Error generating Excel file for ExamId {Id}. StackTrace: {StackTrace}", examUnits.FirstOrDefault()?.ExamId, ex.StackTrace);
                throw;
            }
        }

        private byte[] GenerateWordFile(List<ExamUnit> examUnits, int modelNumber, string bubbleSheetPath, int totalQuestions)
        {
            try
            {
                _logger.LogDebug("Starting Word file generation for model {ModelNumber} with {UnitCount} exam units.", modelNumber, examUnits?.Count ?? 0);

                using var memoryStream = new MemoryStream();
                using (var wordDoc = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document, true))
                {
                    var mainPart = wordDoc.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = mainPart.Document.AppendChild(new Body());

                    // Add bubble sheet as the first page
                    if (!string.IsNullOrEmpty(bubbleSheetPath) && System.IO.File.Exists(bubbleSheetPath))
                    {
                        _logger.LogDebug("Adding bubble sheet image {Path} for model {ModelNumber}.", bubbleSheetPath, modelNumber);
                        AddBubbleSheetPage(body, mainPart, bubbleSheetPath);
                    }
                    else
                    {
                        _logger.LogWarning("Bubble sheet image not found: {Path} for model {ModelNumber}.", bubbleSheetPath, modelNumber);
                    }

                    int questionNumber = 1;
                    char[] choiceLabels = { 'a', 'b', 'c', 'd', 'e', 'f' };

                    // Add footer
                    var footerPart = mainPart.AddNewPart<FooterPart>();
                    var footer = new Footer();
                    var footerParagraph = new Paragraph(
                        new ParagraphProperties(new Justification { Val = JustificationValues.Right }),
                        new Run(new Text($"Model {modelNumber}") { Space = SpaceProcessingModeValues.Preserve })
                    );
                    footer.Append(footerParagraph);
                    footerPart.Footer = footer;

                    var sectionProperties = new SectionProperties();
                    var footerReference = new FooterReference { Type = HeaderFooterValues.Default, Id = mainPart.GetIdOfPart(footerPart) };
                    sectionProperties.Append(footerReference);
                    body.Append(sectionProperties);

                    // Add header table if exam exists
                    if (examUnits?.Any() == true && examUnits.FirstOrDefault()?.Exam != null)
                    {
                        _logger.LogDebug("Adding header table for ExamId {ExamId}.", examUnits.First().Exam.ExamId);
                        var firstExam = examUnits.First().Exam;
                        var material = firstExam.Material ?? new Material();
                        AddHeaderTable(body, firstExam, material, mainPart, totalQuestions);
                    }
                    else
                    {
                        _logger.LogWarning("No exam units or exam data available for model {ModelNumber}.", modelNumber);
                    }

                    AddBoldParagraph(body, "Choose the correct answer for the following:");
                    AddParagraph(body, " ");

                    foreach (var examUnit in examUnits ?? new List<ExamUnit>())
                    {
                        _logger.LogDebug("Processing exam unit for ExamId {ExamId}, GroupId {GroupId}.", examUnit.ExamId, examUnit.GroupId);

                        var problems = examUnit?.Group?.Problems?.ToList();
                        if (problems?.Any() != true)
                        {
                            _logger.LogWarning("No problems found for ExamUnit with ExamId {ExamId}, GroupId {GroupId}. Skipping.", examUnit.ExamId, examUnit.GroupId);
                            continue;
                        }

                        int startQuestion = questionNumber;
                        int endQuestion = questionNumber + problems.Count - 1;
                        string questionRange = startQuestion == endQuestion ? $"From[{startQuestion}]" : $"From[{startQuestion}:{endQuestion}]";

                        if (examUnit?.Group?.HasCommonHeader == true && !string.IsNullOrEmpty(examUnit.Group.CommonQuestionHeader))
                        {
                            _logger.LogDebug("Adding common question header for range {QuestionRange}.", questionRange);
                            body.Append(new Paragraph(
                                new ParagraphProperties(new SpacingBetweenLines { After = "240" }),
                                new Run(
                                    new RunProperties(
                                        new Bold(),
                                        new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = "28" }
                                    ),
                                    new Text($"{questionRange} {examUnit.Group.CommonQuestionHeader}")
                                )
                            ));
                        }

                        foreach (var problem in problems)
                        {
                            _logger.LogDebug("Adding question {QuestionNumber} with ProblemId {ProblemId}.", questionNumber, problem.ProblemId);

                            body.Append(new Paragraph(
                                new ParagraphProperties(new SpacingBetweenLines { After = "120" }),
                                new Run(
                                    new RunProperties(new Bold()),
                                    new Text($"{questionNumber}. {problem.ProblemHeader ?? "N/A"}")
                                )
                            ));

                            if (!string.IsNullOrEmpty(problem.ProblemImagePath))
                            {
                                string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", problem.ProblemImagePath.TrimStart('/'));
                                if (System.IO.File.Exists(imagePath))
                                {
                                    var imageParagraph = new Paragraph(
                                        new ParagraphProperties(new SpacingBetweenLines { After = "120" })
                                    );
                                    var imageRun = AddImageRun(mainPart, imagePath, Path.GetFileName(imagePath), 914400L, 914400L);
                                    imageParagraph.Append(imageRun);
                                    body.Append(imageParagraph);
                                }
                                else
                                {
                                    _logger.LogWarning("Problem image not found at path: {Path}", imagePath);
                                }
                            }

                            var choicesList = problem.ProblemChoices?.OrderBy(c => c.UnitOrder).ToList() ?? new List<ProblemChoice>();
                            if (!choicesList.Any())
                            {
                                _logger.LogWarning("No choices found for ProblemId {ProblemId}.", problem.ProblemId);
                                body.Append(new Paragraph(new Run(new Text("No choices available."))));
                            }
                            else
                            {
                                bool hasLongAnswer = choicesList.Any(c => c.Choices?.Length > 60);
                                if (hasLongAnswer)
                                {
                                    foreach (var choice in choicesList.Select((c, i) => new { Choice = c, Index = i }))
                                    {
                                        body.Append(new Paragraph(
                                            new ParagraphProperties(new SpacingBetweenLines { After = "120" }),
                                            new Run(new Text($"({choiceLabels[choice.Index]}) {choice.Choice.Choices ?? "N/A"}"))
                                        ));
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < choicesList.Count; i += 2)
                                    {
                                        var line = $"({choiceLabels[i]}) {choicesList[i].Choices ?? "N/A"}";
                                        if (i + 1 < choicesList.Count)
                                        {
                                            line = $"{line}{new string(' ', 60 - (line.Length % 60))}({choiceLabels[i + 1]}) {choicesList[i + 1].Choices ?? "N/A"}";
                                        }
                                        body.Append(new Paragraph(
                                            new ParagraphProperties(new SpacingBetweenLines { After = "120" }),
                                            new Run(new Text(line))
                                        ));
                                    }
                                }
                            }

                            questionNumber++;
                        }

                        if (examUnit?.Group?.Problems?.Any() == true)
                        {
                            AddSeparator(body, false);
                        }
                    }

                    AddPageSettings(body);
                    wordDoc.Save();
                }

                _logger.LogDebug("Word file generated for model {ModelNumber} with size {Size} bytes.", modelNumber, memoryStream.Length);
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Word document for model {ModelNumber} for ExamId {ExamId}. StackTrace: {StackTrace}", modelNumber, examUnits?.FirstOrDefault()?.ExamId ?? 0);
                throw new Exception($"Failed to generate Word document for model {modelNumber}.", ex);
            }
        }

        private async Task<List<string>> GenerateBubbleSheets(Exam firstExam, int numberOfModels, int totalQuestions)
        {
            try
            {
                string pythonPath = @"C:\Users\Eng.Ahmed\AppData\Local\Programs\Python\Python312\python.exe"; // Replace with your Python path
                string scriptPath = @"D:\Eng\Graduation_proj\python\Bubble.py"; // Replace with your Bubble.py path
                string outputDir = Path.Combine(_uploadsFolder, "BubbleSheets", Guid.NewGuid().ToString());
                Directory.CreateDirectory(outputDir);

                var material = firstExam.Material ?? new Material();
                string models = string.Join(",", Enumerable.Range(0, numberOfModels).Select(i => ((char)('A' + i)).ToString()));
                string term = material.Term == 1 ? "First Term" : "Second Term";

                var arguments = new[]
                {
                    $"\"{scriptPath}\"",
                    "--title", $"\"{firstExam.ExamName}\"",
                    "--course_name", $"\"{material.MaterialName ?? "N/A"}\"",
                    "--course_code", $"\"{material.MaterialCode ?? "N/A"}\"",
                    "--course_level", $"\"{material.Level ?? "N/A"}\"",
                    "--term", $"\"{term}\"",
                    "--num_questions", totalQuestions.ToString(),
                    "--exam_date", $"\"{firstExam.ExamDate:dd/MM/yyyy}\"",
                    "--full_mark", $"\"{firstExam.MainDegree}\"",
                    "--exam_time", $"\"{(firstExam.ExamDuration / 60.0):0.#} Hours\"",
                    "--department", $"\"{material.Department ?? "N/A"}\"",
                    "--college_name", $"\"{firstExam.CollegeName ?? "N/A"}\"",
                    "--university_name", $"\"{firstExam.UniversityName ?? "N/A"}\"",
                    "--models", $"\"{models}\"",
                    "--output_dir", $"\"{outputDir}\""
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
                        throw new Exception($"Failed to generate bubble sheets: {error}");
                    }

                    _logger.LogInformation("Python script output: {Output}", output);
                }

                return Directory.GetFiles(outputDir, "*.png").ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating bubble sheets for ExamId {ExamId}.", firstExam.ExamId);
                throw;
            }
        }

        private void AddBubbleSheetPage(Body body, MainDocumentPart mainPart, string bubbleSheetPath)
        {
            try
            {
                var imagePart = mainPart.AddImagePart(ImagePartType.Png);
                using (var stream = new FileStream(bubbleSheetPath, FileMode.Open))
                {
                    imagePart.FeedData(stream);
                }

                string imagePartId = mainPart.GetIdOfPart(imagePart);
                long imageWidth = (long)(8.27 * 914400); // A4 width in EMUs
                long imageHeight = (long)(11.69 * 914400); // A4 height in EMUs

                long offsetX = -200000L;
                long offsetY = 90000L;

                var drawing = new Drawing(
                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent { Cx = imageWidth, Cy = imageHeight },
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties { Id = (UInt32Value)(mainPart.Parts.Count() + 1U), Name = "BubbleSheet" },
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties(
                            new DocumentFormat.OpenXml.Drawing.GraphicFrameLocks { NoChangeAspect = true }
                        ),
                        new DocumentFormat.OpenXml.Drawing.Graphic(
                            new DocumentFormat.OpenXml.Drawing.GraphicData(
                                new DocumentFormat.OpenXml.Drawing.Pictures.Picture(
                                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties(
                                        new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties { Id = 0U, Name = Path.GetFileName(bubbleSheetPath) },
                                        new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties()
                                    ),
                                    new DocumentFormat.OpenXml.Drawing.Pictures.BlipFill(
                                        new DocumentFormat.OpenXml.Drawing.Blip { Embed = imagePartId },
                                        new DocumentFormat.OpenXml.Drawing.Stretch(
                                            new DocumentFormat.OpenXml.Drawing.FillRectangle()
                                        )
                                    ),
                                    new DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties(
                                        new DocumentFormat.OpenXml.Drawing.Transform2D(
                                            new DocumentFormat.OpenXml.Drawing.Offset { X = offsetX, Y = offsetY },
                                            new DocumentFormat.OpenXml.Drawing.Extents { Cx = imageWidth, Cy = imageHeight }
                                        ),
                                        new DocumentFormat.OpenXml.Drawing.PresetGeometry(
                                            new DocumentFormat.OpenXml.Drawing.AdjustValueList()
                                        )
                                        { Preset = DocumentFormat.OpenXml.Drawing.ShapeTypeValues.Rectangle }
                                    )
                                )
                            )
                            { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                        )
                    )
                );

                var paragraph = new Paragraph(
                    new ParagraphProperties(
                        new SpacingBetweenLines { Before = "0", After = "0", Line = "0", LineRule = LineSpacingRuleValues.Auto }
                    ),
                    new Run(drawing)
                );

                body.AppendChild(paragraph);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding bubble sheet image to Word document: {ImagePath}. StackTrace: {StackTrace}", bubbleSheetPath, ex.StackTrace);
                throw;
            }
        }

        private void AddHeaderTable(Body body, Exam firstExam, Material material, MainDocumentPart mainPart, int totalQuestions)
        {
            try
            {
                var table = new Table();
                var tableProperties = new TableProperties(
                    new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                    new TableJustification { Val = TableRowAlignmentValues.Center }
                );
                table.AppendChild(tableProperties);

                var row1 = new TableRow();
                row1.Append(
                    CreateTableCell($"Course Name: {material.MaterialName ?? "N/A"}", true, "auto", JustificationValues.Left, true, false),
                    CreateTableCell($"Faculty of {firstExam.CollegeName ?? "N/A"}", false, "50%", JustificationValues.Center, false, false),
                    CreateTableCell($"{firstExam.ExamName ?? "N/A"}", true, "auto", JustificationValues.Left, true, false)
                );
                table.AppendChild(row1);

                var row2 = new TableRow();
                row2.Append(
                    CreateTableCell($"Course Code: {material.MaterialCode ?? "N/A"}", false, "auto", JustificationValues.Left, true, false),
                    CreateTableCell($"{firstExam.UniversityName ?? "N/A"} University", false, "50%", JustificationValues.Center, false, false),
                    CreateTableCell($"Date: {firstExam.ExamDate:dd/MM/yyyy}", false, "auto", JustificationValues.Left, true, false)
                );
                table.AppendChild(row2);

                var row3 = new TableRow();
                row3.Append(
                    CreateTableCell($"Course Level: {material.Level ?? "N/A"}", false, "auto", JustificationValues.Left, true, false),
                    CreateTableCell("", false, "auto", JustificationValues.Center, false, false),
                    CreateTableCell($"Full Mark: {firstExam.MainDegree}", false, "auto", JustificationValues.Left, true, false)
                );
                table.AppendChild(row3);

                var row4 = new TableRow();
                row4.Append(
                    CreateTableCell($"Term: {(material.Term == 1 ? "First Term" : "Second Term")}", false, "auto", JustificationValues.Left, true, false),
                    CreateTableCell("", false, "auto", JustificationValues.Center, false, false),
                    CreateTableCell($"No. of Questions: {totalQuestions}", false, "auto", JustificationValues.Left, true, false)
                );
                table.AppendChild(row4);

                var row5 = new TableRow();
                var departmentCell = CreateTableCell($"Department: {material.Department ?? "N/A"}", false, "auto", JustificationValues.Left, true, true);
                departmentCell.TableCellProperties.Append(new GridSpan { Val = 2 });
                row5.Append(departmentCell);
                row5.Append(CreateTableCell($"Time: {(firstExam.ExamDuration / 60.0):0.#} Hours", false, "auto", JustificationValues.Left, true, true));
                table.AppendChild(row5);

                body.AppendChild(table);

                string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "zag.png");
                if (System.IO.File.Exists(imagePath))
                {
                    AddImageToBody(body, mainPart, imagePath, JustificationValues.Center);
                }
                else
                {
                    _logger.LogWarning("Header image not found: {ImagePath}", imagePath);
                }

                AddSeparator(body, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating header table for ExamId {ExamId}. StackTrace: {StackTrace}", firstExam.ExamId, ex.StackTrace);
                throw;
            }
        }

        private TableCell CreateTableCell(string text, bool isBold, string width, JustificationValues alignment, bool isAutoWidth, bool isLastRow)
        {
            var run = new Run(new Text(text));
            run.RunProperties = new RunProperties(
                new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = "26" },
                new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" }
            );
            if (isBold)
                run.RunProperties.Append(new Bold());

            var paragraph = new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = alignment },
                    new SpacingBetweenLines { Before = "50", After = isLastRow ? "0" : "50", Line = "240", LineRule = LineSpacingRuleValues.Auto }
                ),
                run
            );

            var cellProperties = new TableCellProperties(
                new TableCellWidth { Width = isAutoWidth ? "0" : width, Type = isAutoWidth ? TableWidthUnitValues.Auto : TableWidthUnitValues.Pct },
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
            );

            return new TableCell(cellProperties, paragraph);
        }

        private void AddImageToBody(Body body, MainDocumentPart mainPart, string imagePath, JustificationValues alignment)
        {
            try
            {
                var imagePart = mainPart.AddImagePart(ImagePartType.Png);
                using (var stream = new FileStream(imagePath, FileMode.Open))
                {
                    imagePart.FeedData(stream);
                }

                string imagePartId = mainPart.GetIdOfPart(imagePart);
                long imageWidth = 731520L;
                long imageHeight = 731520L;
                long horizontalOffset = 3500000L;
                long verticalOffset = -750000L;

                var drawing = new Drawing(
                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor(
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.SimplePosition { X = 0L, Y = 0L },
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.HorizontalPosition(
                            new DocumentFormat.OpenXml.Drawing.Wordprocessing.PositionOffset(horizontalOffset.ToString())
                        )
                        { RelativeFrom = DocumentFormat.OpenXml.Drawing.Wordprocessing.HorizontalRelativePositionValues.Margin },
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.VerticalPosition(
                            new DocumentFormat.OpenXml.Drawing.Wordprocessing.PositionOffset(verticalOffset.ToString())
                        )
                        { RelativeFrom = DocumentFormat.OpenXml.Drawing.Wordprocessing.VerticalRelativePositionValues.Paragraph },
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent { Cx = imageWidth, Cy = imageHeight },
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapNone(),
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties { Id = (UInt32Value)1U, Name = "ZagLogo" },
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties(
                            new DocumentFormat.OpenXml.Drawing.GraphicFrameLocks { NoChangeAspect = true }
                        ),
                        new DocumentFormat.OpenXml.Drawing.Graphic(
                            new DocumentFormat.OpenXml.Drawing.GraphicData(
                                new DocumentFormat.OpenXml.Drawing.Pictures.Picture(
                                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties(
                                        new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties { Id = 0U, Name = Path.GetFileName(imagePath) },
                                        new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties()
                                    ),
                                    new DocumentFormat.OpenXml.Drawing.Pictures.BlipFill(
                                        new DocumentFormat.OpenXml.Drawing.Blip { Embed = imagePartId },
                                        new DocumentFormat.OpenXml.Drawing.Stretch(
                                            new DocumentFormat.OpenXml.Drawing.FillRectangle()
                                        )
                                    ),
                                    new DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties(
                                        new DocumentFormat.OpenXml.Drawing.Transform2D(
                                            new DocumentFormat.OpenXml.Drawing.Offset { X = 0L, Y = 0L },
                                            new DocumentFormat.OpenXml.Drawing.Extents { Cx = imageWidth, Cy = imageHeight }
                                        ),
                                        new DocumentFormat.OpenXml.Drawing.PresetGeometry(
                                            new DocumentFormat.OpenXml.Drawing.AdjustValueList()
                                        )
                                        { Preset = DocumentFormat.OpenXml.Drawing.ShapeTypeValues.Rectangle }
                                    )
                                )
                            )
                            { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                        )
                    )
                    {
                        DistanceFromTop = (UInt32Value)0U,
                        DistanceFromBottom = (UInt32Value)0U,
                        DistanceFromLeft = (UInt32Value)0U,
                        DistanceFromRight = (UInt32Value)0U,
                        SimplePos = false,
                        RelativeHeight = (UInt32Value)0U,
                        BehindDoc = true,
                        Locked = false,
                        LayoutInCell = true,
                        AllowOverlap = true
                    }
                );

                var paragraph = new Paragraph(
                    new ParagraphProperties(
                        new Justification { Val = alignment },
                        new SpacingBetweenLines { Before = "0", After = "0", Line = "0", LineRule = LineSpacingRuleValues.Auto }
                    ),
                    new Run(drawing)
                );

                body.AppendChild(paragraph);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding image to Word document: {ImagePath}. StackTrace: {StackTrace}", imagePath, ex.StackTrace);
                throw;
            }
        }

        private Run AddImageRun(MainDocumentPart mainPart, string imagePath, string imageName, long imageWidth, long imageHeight)
        {
            var extension = System.IO.Path.GetExtension(imagePath).ToLower();
            var imagePartType = extension == ".jpg" || extension == ".jpeg" ? ImagePartType.Jpeg : ImagePartType.Png;
            var imagePart = mainPart.AddImagePart(imagePartType);

            using (var stream = new FileStream(imagePath, FileMode.Open))
            {
                imagePart.FeedData(stream);
            }

            string imagePartId = mainPart.GetIdOfPart(imagePart);

            var drawing = new Drawing(
                new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(
                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent() { Cx = imageWidth, Cy = imageHeight },
                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.EffectExtent()
                    {
                        LeftEdge = 0L,
                        TopEdge = 0L,
                        RightEdge = 0L,
                        BottomEdge = 0L
                    },
                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties()
                    {
                        Id = (UInt32Value)(mainPart.Parts.Count() + 1U),
                        Name = imageName
                    },
                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties(
                        new DocumentFormat.OpenXml.Drawing.GraphicFrameLocks() { NoChangeAspect = true }
                    ),
                    new DocumentFormat.OpenXml.Drawing.Graphic(
                        new DocumentFormat.OpenXml.Drawing.GraphicData(
                            new DocumentFormat.OpenXml.Drawing.Pictures.Picture(
                                new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties(
                                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties()
                                    {
                                        Id = 0U,
                                        Name = imageName
                                    },
                                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties()
                                ),
                                new DocumentFormat.OpenXml.Drawing.Pictures.BlipFill(
                                    new DocumentFormat.OpenXml.Drawing.Blip() { Embed = imagePartId },
                                    new DocumentFormat.OpenXml.Drawing.Stretch(
                                        new DocumentFormat.OpenXml.Drawing.FillRectangle()
                                    )
                                ),
                                new DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties(
                                    new DocumentFormat.OpenXml.Drawing.Transform2D(
                                        new DocumentFormat.OpenXml.Drawing.Offset() { X = 0L, Y = 0L },
                                        new DocumentFormat.OpenXml.Drawing.Extents() { Cx = imageWidth, Cy = imageHeight }
                                    ),
                                    new DocumentFormat.OpenXml.Drawing.PresetGeometry(
                                        new DocumentFormat.OpenXml.Drawing.AdjustValueList()
                                    )
                                    { Preset = DocumentFormat.OpenXml.Drawing.ShapeTypeValues.Rectangle }
                                )
                            )
                        )
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                    )
                )
            );

            return new Run(drawing);
        }

        private void AddParagraph(Body body, string text, int fontSize = 14)
        {
            var run = new Run(new Text(text));
            run.RunProperties = new RunProperties(
                new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = (fontSize * 2).ToString() }
            );

            var paragraphProperties = new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto }
            );

            var paragraph = new Paragraph(paragraphProperties, run);
            body.AppendChild(paragraph);
        }

        private void AddBoldParagraph(Body body, string text, int fontSize = 14)
        {
            var run = new Run(new Text(text));
            run.RunProperties = new RunProperties(
                new Bold(),
                new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = (fontSize * 2).ToString() }
            );

            var paragraphProperties = new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto }
            );

            var paragraph = new Paragraph(paragraphProperties, run);
            body.AppendChild(paragraph);
        }

        private void AddSeparator(Body body, bool isMainSeparator)
        {
            var paragraphProperties = new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "0", Line = "100", LineRule = LineSpacingRuleValues.Auto }
            );

            uint size = isMainSeparator ? 30u : 12u;
            paragraphProperties.Append(new ParagraphBorders
            {
                BottomBorder = new BottomBorder { Val = BorderValues.Single, Size = size, Color = "000000" }
            });

            var paragraph = new Paragraph(paragraphProperties);
            body.AppendChild(paragraph);
        }

        private void AddPageSettings(Body body)
        {
            var sectionProperties = new SectionProperties();
            var pageBorders = new PageBorders
            {
                TopBorder = new TopBorder { Val = BorderValues.Single, Size = 48, Space = 10, Color = "000000" },
                BottomBorder = new BottomBorder { Val = BorderValues.Single, Size = 48, Space = 10, Color = "000000" },
                LeftBorder = new LeftBorder { Val = BorderValues.Single, Size = 48, Space = 10, Color = "000000" },
                RightBorder = new RightBorder { Val = BorderValues.Single, Size = 48, Space = 10, Color = "000000" }
            };
            var pageMargin = new PageMargin { Top = 500, Bottom = 500, Left = 500, Right = 500 };
            sectionProperties.Append(pageBorders, pageMargin);
            body.AppendChild(sectionProperties);
        }

        private List<ExamUnit> ShuffleExamUnits(List<ExamUnit> examUnits)
        {
            try
            {
                var random = new Random();
                var finalExamUnits = examUnits.Select(eu => new ExamUnit
                {
                    ExamId = eu.ExamId,
                    Exam = eu.Exam,
                    Group = new Group
                    {
                        GroupId = eu.Group?.GroupId ?? 0,
                        GroupName = eu.Group?.GroupName,
                        CommonQuestionHeader = eu.Group?.CommonQuestionHeader,
                        HasCommonHeader = eu.Group?.HasCommonHeader ?? false,
                        Problems = eu.Group?.Problems?.Select(p => new Problem
                        {
                            ProblemId = p.ProblemId,
                            ProblemHeader = p.ProblemHeader,
                            ProblemImagePath = p.ProblemImagePath,
                            RightAnswer = p.RightAnswer,
                            Shuffle = p.Shuffle,
                            ProblemChoices = p.ProblemChoices?.Select(c => new ProblemChoice
                            {
                                ChoiceId = c.ChoiceId,
                                Choices = c.Choices,
                                ChoiceImagePath = c.ChoiceImagePath,
                                UnitOrder = c.UnitOrder,
                                ProblemId = c.ProblemId
                            }).ToList() ?? new List<ProblemChoice>()
                        }).ToList() ?? new List<Problem>()
                    },
                    Shuffle = eu.Shuffle,
                    UnitOrder = eu.UnitOrder,
                    MainDegree = eu.MainDegree,
                    TotalProblems = eu.TotalProblems,
                    AllProblems = eu.AllProblems
                }).ToList();

                return finalExamUnits.OrderBy(_ => random.Next()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shuffling exam units for ExamId {Id}. StackTrace: {StackTrace}", examUnits.FirstOrDefault()?.ExamId, ex.StackTrace);
                throw;
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

        private List<string> GetModelStateErrors()
        {
            return ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
        }
    }

    public class GenerateExamFilesDto
    {
        [Required(ErrorMessage = "Exam ID is required.")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Number of models is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Number of models must be at least 1.")]
        public int NumberOfModels { get; set; }
    }
}