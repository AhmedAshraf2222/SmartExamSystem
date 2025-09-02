using Graduation_proj.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Graduation_proj.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExamsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExamsController> _logger;

        public ExamsController(ApplicationDbContext context, ILogger<ExamsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves a list of all exams.
        /// </summary>
        /// <returns>A list of exams with their details.</returns>
        [HttpGet]
        public async Task<IActionResult> GetExams()
        {
            try
            {
                var exams = await _context.Exams
                    .Include(e => e.Material)
                    .Select(e => new ExamDto
                    {
                        ExamId = e.ExamId,
                        ExamName = e.ExamName,
                        MaterialId = e.MaterialId,
                        MaterialName = e.Material != null ? e.Material.MaterialName : "N/A",
                        MainDegree = e.MainDegree,
                        TotalProblems = e.TotalProblems,
                        Shuffle = e.Shuffle,
                        ExamDuration = e.ExamDuration,
                        ExamDate = e.ExamDate,
                        UniversityName = e.UniversityName,
                        CollegeName = e.CollegeName
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} exams.", exams.Count);
                return Ok(exams);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving exams.");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving exams." });
            }
        }

        /// <summary>
        /// Retrieves a specific exam by its ID.
        /// </summary>
        /// <param name="id">The ID of the exam to retrieve.</param>
        /// <returns>The exam details if found; otherwise, a 404 error.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetExam(int id)
        {
            try
            {
                var exam = await _context.Exams
                    .Include(e => e.Material)
                    .FirstOrDefaultAsync(e => e.ExamId == id);

                if (exam == null)
                {
                    _logger.LogWarning("Exam with ID {Id} not found.", id);
                    return NotFound(new { success = false, message = "Exam not found." });
                }

                var examDto = new ExamDto
                {
                    ExamId = exam.ExamId,
                    ExamName = exam.ExamName,
                    MaterialId = exam.MaterialId,
                    MaterialName = exam.Material != null ? exam.Material.MaterialName : "N/A",
                    MainDegree = exam.MainDegree,
                    TotalProblems = exam.TotalProblems,
                    Shuffle = exam.Shuffle,
                    ExamDuration = exam.ExamDuration,
                    ExamDate = exam.ExamDate,
                    UniversityName = exam.UniversityName,
                    CollegeName = exam.CollegeName
                };

                return Ok(examDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving exam with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving the exam." });
            }
        }

        /// <summary>
        /// Adds a new exam.
        /// </summary>
        /// <param name="examDto">The exam data to add.</param>
        /// <returns>The created exam details with a 201 status code.</returns>
        [HttpPost]
        public async Task<IActionResult> AddExam([FromBody] CreateExamDto examDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for AddExam: {Errors}", string.Join(", ", errors));
                return BadRequest(new { success = false, errors });
            }

            try
            {
                var materialExists = await _context.Materials.AnyAsync(m => m.MaterialId == examDto.MaterialId);
                if (!materialExists)
                {
                    _logger.LogWarning("Material with ID {MaterialId} not found.", examDto.MaterialId);
                    return BadRequest(new { success = false, message = "Invalid Material ID. Material does not exist." });
                }

                var exam = new Exam
                {
                    ExamName = examDto.ExamName,
                    MaterialId = examDto.MaterialId,
                    Material = null,
                    MainDegree = examDto.MainDegree,
                    TotalProblems = examDto.TotalProblems,
                    Shuffle = examDto.Shuffle,
                    ExamDuration = examDto.ExamDuration,
                    ExamDate = examDto.ExamDate,
                    UniversityName = examDto.UniversityName,
                    CollegeName = examDto.CollegeName
                };

                _context.Exams.Add(exam);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Exam {ExamName} created with ID {ExamId}.", exam.ExamName, exam.ExamId);
                return CreatedAtAction(nameof(GetExam), new { id = exam.ExamId }, new
                {
                    success = true,
                    message = "Exam created successfully.",
                    examId = exam.ExamId
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while adding exam {ExamName}.", examDto.ExamName);
                return StatusCode(500, new { success = false, message = "An error occurred while adding the exam." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding exam {ExamName}.", examDto.ExamName);
                return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Updates an existing exam.
        /// </summary>
        /// <param name="id">The ID of the exam to update.</param>
        /// <param name="examDto">The updated exam data.</param>
        /// <returns>A success message if updated; otherwise, a 404 or 400 error.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> EditExam(int id, [FromBody] CreateExamDto examDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for EditExam: {Errors}", string.Join(", ", errors));
                return BadRequest(new { success = false, errors });
            }

            try
            {
                var exam = await _context.Exams.FirstOrDefaultAsync(e => e.ExamId == id);
                if (exam == null)
                {
                    _logger.LogWarning("Exam with ID {Id} not found for update.", id);
                    return NotFound(new { success = false, message = "Exam not found." });
                }

                var materialExists = await _context.Materials.AnyAsync(m => m.MaterialId == examDto.MaterialId);
                if (!materialExists)
                {
                    _logger.LogWarning("Material with ID {MaterialId} not found for exam update.", examDto.MaterialId);
                    return BadRequest(new { success = false, message = "Invalid Material ID. Material does not exist." });
                }

                exam.ExamName = examDto.ExamName;
                exam.MaterialId = examDto.MaterialId;
                exam.Material = null;
                exam.MainDegree = examDto.MainDegree;
                exam.TotalProblems = examDto.TotalProblems;
                exam.Shuffle = examDto.Shuffle;
                exam.ExamDuration = examDto.ExamDuration;
                exam.ExamDate = examDto.ExamDate;
                exam.UniversityName = examDto.UniversityName;
                exam.CollegeName = examDto.CollegeName;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Exam {ExamName} with ID {Id} updated.", exam.ExamName, id);
                return Ok(new { success = true, message = "Exam updated successfully." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating exam with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An error occurred while updating the exam." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating exam with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Deletes an exam by its ID.
        /// </summary>
        /// <param name="id">The ID of the exam to delete.</param>
        /// <returns>A success message if deleted; otherwise, a 404 error.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExam(int id)
        {
            try
            {
                var exam = await _context.Exams.FindAsync(id);
                if (exam == null)
                {
                    _logger.LogWarning("Exam with ID {Id} not found for deletion.", id);
                    return NotFound(new { success = false, message = "Exam not found." });
                }

                _context.Exams.Remove(exam);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Exam with ID {Id} deleted.", id);
                return Ok(new { success = true, message = "Exam deleted successfully." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while deleting exam with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An error occurred while deleting the exam." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting exam with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Retrieves a list of all materials.
        /// </summary>
        /// <returns>A list of materials with their IDs and names.</returns>
        [HttpGet("materials")]
        public async Task<IActionResult> GetMaterials()
        {
            try
            {
                var materials = await _context.Materials
                    .Select(m => new
                    {
                        m.MaterialId,
                        m.MaterialName
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} materials.", materials.Count);
                return Ok(materials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving materials.");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving materials." });
            }
        }
    }

    public class ExamDto
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; }
        public int MaterialId { get; set; }
        public string MaterialName { get; set; }
        public int MainDegree { get; set; }
        public int TotalProblems { get; set; }
        public bool Shuffle { get; set; }
        public int ExamDuration { get; set; }
        public DateTime ExamDate { get; set; }
        public string UniversityName { get; set; }
        public string CollegeName { get; set; }
    }

    public class CreateExamDto
    {
        [Required(ErrorMessage = "Exam name is required.")]
        [MaxLength(40, ErrorMessage = "Exam name cannot exceed 40 characters.")]
        public string ExamName { get; set; }

        [Required(ErrorMessage = "Material ID is required.")]
        public int MaterialId { get; set; }

        [Required(ErrorMessage = "Main degree is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Main degree must be a positive number.")]
        public int MainDegree { get; set; }

        [Required(ErrorMessage = "Total problems is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Total problems must be a positive number.")]
        public int TotalProblems { get; set; }

        public bool Shuffle { get; set; }

        [Required(ErrorMessage = "Exam duration is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Exam duration must be a positive number.")]
        public int ExamDuration { get; set; }

        [Required(ErrorMessage = "Exam date is required.")]
        public DateTime ExamDate { get; set; }

        [MaxLength(100, ErrorMessage = "University name cannot exceed 100 characters.")]
        public string UniversityName { get; set; }

        [MaxLength(100, ErrorMessage = "College name cannot exceed 100 characters.")]
        public string CollegeName { get; set; }
    }
}