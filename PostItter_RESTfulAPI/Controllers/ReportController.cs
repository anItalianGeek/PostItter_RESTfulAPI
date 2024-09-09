using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PostItter_RESTfulAPI.Entity;

namespace PostItter_RESTfulAPI.Controllers;

[ApiController]
[Route("api")]
public class ReportController : ControllerBase
{
    [HttpPost("submitReport")]
    public async Task<IActionResult> submitReport([FromBody] Report report)
    {
        try
        {
            if (report == null || report.reportedUser == null || report.explanation.IsNullOrEmpty() || report.reason.IsNullOrEmpty())
            {
                return BadRequest("Invalid report data.");
            }

            // Generate a unique folder name using the username
            string folderPath = Path.Combine("reports", $"report-{report.reportedUser.username}");
            Directory.CreateDirectory(folderPath);

            // Add a timestamp to the file name to ensure uniqueness
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            string filePath = Path.Combine(folderPath, $"report-{timestamp}.txt");

            // Format the report content
            string reportContent = $"Reason: {report.reason}\nExplanation: {report.explanation}\nReported by: {report.reportedUser.username}, {report.reportedUser.displayName}, {report.reported_by.id}";

            // Write the report content to the file asynchronously
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                await writer.WriteAsync(reportContent);
            }

            return Ok(new { message = "Report submitted successfully." });
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

    [HttpPost("contact")]
    public async Task<IActionResult> contactDevs([FromBody] Contact contactInfo)
    {
        try
        {
            // Combina il percorso della cartella
            string folderPath = Path.Combine("contacts", $"{contactInfo.lastName}-{contactInfo.firstName}");
            Directory.CreateDirectory(folderPath);

            // Aggiunge un timestamp al nome del file per garantire l'unicità
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            string filePath = Path.Combine(folderPath, $"contact-{timestamp}.txt");

            // Format il contenuto del contatto
            string fileContent =
                $"First Name: {contactInfo.firstName}\nLast Name: {contactInfo.lastName}\nNumber: {contactInfo.number}\nMessage: {contactInfo.content}";

            // Scrive il contenuto del contatto nel file in modo asincrono
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                await writer.WriteAsync(fileContent);
            }
            
            return Ok();
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }

    }
}