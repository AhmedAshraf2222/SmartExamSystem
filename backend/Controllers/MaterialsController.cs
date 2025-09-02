using Graduation_proj.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Graduation_proj.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaterialsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MaterialsController> _logger;

        public MaterialsController(ApplicationDbContext context, ILogger<MaterialsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves all materials with associated doctor names.
        /// </summary>
        /// <returns>A list of materials.</returns>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMaterials()
        {
            try
            {
                var doctorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var materials = await _context.Materials
                    .Include(m => m.Doctor)
                    .Where(m => m.DoctorId == doctorId)
                    .Select(m => new MaterialDto
                    {
                        MaterialId = m.MaterialId,
                        MaterialName = m.MaterialName,
                        MaterialCode = m.MaterialCode,
                        Level = m.Level,
                        Department = m.Department,
                        Term = m.Term,
                        DoctorId = m.DoctorId,
                        DoctorName = m.Doctor != null ? m.Doctor.Name : null
                    })
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} materials for doctor ID {DoctorId}.", materials.Count, doctorId);
                return Ok(materials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving materials for doctor ID {DoctorId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving materials." });
            }
        }

        /// <summary>
        /// Retrieves a single material by ID.
        /// </summary>
        /// <param name="id">The material ID.</param>
        /// <returns>The requested material if found; otherwise, an error.</returns>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetMaterial(int id)
        {
            try
            {
                var doctorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var material = await _context.Materials
                    .Include(m => m.Doctor)
                    .FirstOrDefaultAsync(m => m.MaterialId == id && m.DoctorId == doctorId);

                if (material == null)
                {
                    _logger.LogWarning("Material ID {MaterialId} not found for doctor ID {DoctorId}.", id, doctorId);
                    return NotFound(new { success = false, message = "Material not found." });
                }

                return Ok(new MaterialDto
                {
                    MaterialId = material.MaterialId,
                    MaterialName = material.MaterialName,
                    MaterialCode = material.MaterialCode,
                    Level = material.Level,
                    Department = material.Department,
                    Term = material.Term,
                    DoctorId = material.DoctorId,
                    DoctorName = material.Doctor?.Name
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving material ID {MaterialId}.", id);
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving the material." });
            }
        }

        /// <summary>
        /// Creates a new material.
        /// </summary>
        /// <param name="materialDto">The material data.</param>
        /// <returns>The created material's ID.</returns>
        [HttpPost]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateMaterial([FromBody] CreateMaterialDto materialDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for CreateMaterial: {Errors}", string.Join(", ", errors));
                return BadRequest(new { success = false, errors });
            }

            try
            {
                var doctorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var material = new Material
                {
                    MaterialName = materialDto.MaterialName,
                    MaterialCode = materialDto.MaterialCode,
                    Level = materialDto.Level,
                    Department = materialDto.Department,
                    Term = materialDto.Term,
                    DoctorId = doctorId 
                };

                _context.Materials.Add(material);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Material {MaterialName} created with ID {MaterialId} by doctor ID {DoctorId}.", material.MaterialName, material.MaterialId, doctorId);
                return Ok(new { success = true, message = "Material created successfully.", materialId = material.MaterialId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating material.");
                return StatusCode(500, new { success = false, message = "An error occurred while creating the material." });
            }
        }


        /// <summary>
        /// Updates an existing material.
        /// </summary>
        /// <param name="id">The material ID.</param>
        /// <param name="materialDto">The updated material data.</param>
        /// <returns>A success message if updated.</returns>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateMaterial(int id, [FromBody] UpdateMaterialDto materialDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for UpdateMaterial: {Errors}", string.Join(", ", errors));
                return BadRequest(new { success = false, errors });
            }

            try
            {
                var doctorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var material = await _context.Materials.FindAsync(id);
                if (material == null || material.DoctorId != doctorId)
                {
                    return Unauthorized(new { success = false, message = "Unauthorized or material not found." });
                }

                material.MaterialName = materialDto.MaterialName;
                material.MaterialCode = materialDto.MaterialCode;
                material.Level = materialDto.Level;
                material.Department = materialDto.Department;
                material.Term = materialDto.Term;

                _context.Materials.Update(material);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Material ID {MaterialId} updated by doctor ID {DoctorId}.", id, doctorId);
                return Ok(new { success = true, message = "Material updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating material.");
                return StatusCode(500, new { success = false, message = "An error occurred while updating the material." });
            }
        }


        /// <summary>
        /// Deletes a material.
        /// </summary>
        /// <param name="id">The material ID.</param>
        /// <returns>A success message if deleted.</returns>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteMaterial(int id)
        {
            try
            {
                var doctorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var material = await _context.Materials.FindAsync(id);
                if (material == null)
                {
                    _logger.LogWarning("Material ID {MaterialId} not found.", id);
                    return NotFound(new { success = false, message = "Material not found." });
                }

                if (material.DoctorId != doctorId)
                {
                    _logger.LogWarning("Doctor ID {DoctorId} attempted to delete material ID {MaterialId} owned by another doctor.", doctorId, id);
                    return Unauthorized(new { success = false, message = "You can only delete your own materials." });
                }

                _context.Materials.Remove(material);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Material ID {MaterialId} deleted by doctor ID {DoctorId}.", id, doctorId);
                return Ok(new { success = true, message = "Material deleted successfully." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while deleting material ID {MaterialId}.", id);
                return StatusCode(500, new { success = false, message = "An error occurred while deleting the material." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting material ID {MaterialId}.", id);
                return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
            }
        }
    }

    public class MaterialDto
    {
        public int MaterialId { get; set; }
        public string MaterialName { get; set; }
        public string MaterialCode { get; set; }
        public string Level { get; set; }
        public string Department { get; set; }
        public int Term { get; set; }
        public int? DoctorId { get; set; }
        public string DoctorName { get; set; }
    }

    public class CreateMaterialDto
    {
        public string MaterialName { get; set; }
        public string MaterialCode { get; set; }
        public string Level { get; set; }
        public string Department { get; set; }
        public int Term { get; set; }
    }

    public class UpdateMaterialDto
    {
        public string MaterialName { get; set; }
        public string MaterialCode { get; set; }
        public string Level { get; set; }
        public string Department { get; set; }
        public int Term { get; set; }
    }


}