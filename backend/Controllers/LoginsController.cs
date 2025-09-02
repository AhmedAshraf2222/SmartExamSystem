using Graduation_proj.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;

namespace Graduation_proj.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginsController> _logger;

        public LoginsController(ApplicationDbContext context, IConfiguration configuration, ILogger<LoginsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Authenticates a doctor and returns a JWT token.
        /// </summary>
        /// <param name="docDTO">The doctor's login credentials.</param>
        /// <returns>A JWT token and doctor details if authenticated; otherwise, an error.</returns>
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] DocDTO docDTO)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for Login: {Errors}", string.Join(", ", errors));
                return BadRequest(new { success = false, errors });
            }

            try
            {
                var existingDoctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.Email == docDTO.Email);

                if (existingDoctor == null || existingDoctor.Password != docDTO.Password)
                {
                    _logger.LogWarning("Invalid login attempt for email {Email}.", docDTO.Email);
                    return Unauthorized(new { success = false, message = "Invalid email or password." });
                }

                var token = GenerateJwtToken(existingDoctor);
                _logger.LogInformation("Doctor {Email} logged in successfully.", existingDoctor.Email);

                return Ok(new
                {
                    success = true,
                    message = "Login successful.",
                    token,
                    doctor = new DoctorDto
                    {
                        DoctorId = existingDoctor.DoctorId,
                        Name = existingDoctor.Name,
                        Email = existingDoctor.Email
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email {Email}.", docDTO.Email);
                return StatusCode(500, new { success = false, message = "An error occurred during login." });
            }
        }

        /// <summary>
        /// Registers a new doctor.
        /// </summary>
        /// <param name="registerDto">The doctor's registration data.</param>
        /// <returns>A success message if registered; otherwise, an error.</returns>
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterDoctorDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for Register: {Errors}", string.Join(", ", errors));
                return BadRequest(new { success = false, errors });
            }

            try
            {
                var emailExists = await _context.Doctors.AnyAsync(d => d.Email == registerDto.Email);
                if (emailExists)
                {
                    _logger.LogWarning("Email {Email} is already registered.", registerDto.Email);
                    return Conflict(new { success = false, message = "Email already exists." });
                }

                var doctor = new Doctor
                {
                    Name = registerDto.Name,
                    Email = registerDto.Email,
                    Password = registerDto.Password,
                };

                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Doctor {Email} registered with ID {DoctorId}.", doctor.Email, doctor.DoctorId);
                return CreatedAtAction(nameof(Login), new
                {
                    success = true,
                    message = "Registration successful. Please login.",
                    doctorId = doctor.DoctorId
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while registering doctor {Email}.", registerDto.Email);
                return StatusCode(500, new { success = false, message = "An error occurred while registering the doctor." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while registering doctor {Email}.", registerDto.Email);
                return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
            }
        }

        private string GenerateJwtToken(Doctor doctor)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, doctor.DoctorId.ToString()),
                new Claim(ClaimTypes.Name, doctor.Name),
                new Claim(ClaimTypes.Email, doctor.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Gets doctor by ID
        /// </summary>
        [HttpGet("Doctors/{id}")]
        public async Task<IActionResult> GetDoctor(int id)
        {
            try
            {
                var doctor = await _context.Doctors.FindAsync(id);
                if (doctor == null)
                {
                    return NotFound(new { success = false, message = "Doctor not found" });
                }

                return Ok(new DoctorDto
                {
                    DoctorId = doctor.DoctorId,
                    Name = doctor.Name,
                    Email = doctor.Email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting doctor with ID {DoctorId}", id);
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// Updates doctor profile
        /// </summary>
        [HttpPut("Doctors/{id}")]
        public async Task<IActionResult> UpdateDoctor(int id, [FromBody] UpdateDoctorDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid data" });
            }

            try
            {
                var doctor = await _context.Doctors.FindAsync(id);
                if (doctor == null)
                {
                    return NotFound(new { success = false, message = "Doctor not found" });
                }

                if (await _context.Doctors.AnyAsync(d => d.Email == updateDto.Email && d.DoctorId != id))
                {
                    return Conflict(new { success = false, message = "Email already in use" });
                }

                // تحديث البيانات
                doctor.Name = updateDto.Name;
                doctor.Email = updateDto.Email;

                // إذا كانت هناك كلمة مرور جديدة
                if (!string.IsNullOrEmpty(updateDto.Password))
                {
                    doctor.Password = updateDto.Password;
                }

                await _context.SaveChangesAsync();

                // إنشاء توكن جديد إذا تم تغيير كلمة المرور
                string newToken = null;
                if (!string.IsNullOrEmpty(updateDto.Password))
                {
                    newToken = GenerateJwtToken(doctor);
                }

                return Ok(new
                {
                    success = true,
                    message = "Profile updated successfully",
                    token = newToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating doctor with ID {DoctorId}", id);
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

       
    }

    public class DoctorDto
    {
        public int DoctorId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class DocDTO
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }
    }

    public class RegisterDoctorDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; }
    }
    public class UpdateDoctorDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [MinLength(6)]
        public string Password { get; set; }
    }
}