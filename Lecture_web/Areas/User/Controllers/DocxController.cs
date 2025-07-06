using Lecture_web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Lecture_web.Service;
using Lecture_web.Models;
using System.Text.RegularExpressions;

namespace Lecture_web.Areas.User.Controllers
{
    [Route("api/[controller]")]
    [Area("User")]
    [Authorize(Roles = "Giangvien")]
    public class DocxController : ControllerBase
    {
        private readonly ConvertWordToHTML _convertWordToHTML;

        public DocxController(ConvertWordToHTML wordToHtml)
        {
            _convertWordToHTML = wordToHtml;
        }
        [HttpPost("convert")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> Convert([FromForm] IFormFile file)
        {
            if (file == null || !file.FileName.EndsWith(".docx"))
                return BadRequest("Vui lòng upload file .docx");

            string fullHtml;
            using (var stream = file.OpenReadStream())
            {
                try
                {
                    fullHtml = await _convertWordToHTML.ConvertAsync(stream);
                }
                catch (System.Exception ex)
                {
                    return StatusCode(500, ex.Message);
                }
            }

            // 1) Trích style block
            var styleMatch = Regex.Match(
                fullHtml,
                @"<style\b[^>]*>[\s\S]*?<\/style>",
                RegexOptions.IgnoreCase
            );
            var styleBlock = styleMatch.Success
                ? styleMatch.Value
                : string.Empty;

            // 2) Loại bỏ style, meta, title để lấy riêng phần nội dung
            var htmlOnly = fullHtml;

            // Xóa style block
            if (styleMatch.Success)
                htmlOnly = htmlOnly.Replace(styleMatch.Value, "");

            // Xóa các thẻ meta/title phát sinh
            htmlOnly = Regex.Replace(
                htmlOnly,
                @"<(meta|title)\b[^>]*>.*?<\/\1>|<meta\b[^>]*\/?>",
                "",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            );

            // Trả về JSON với 2 trường riêng biệt
            return Ok(new
            {
                styleBlock,
                html = htmlOnly.Trim()
            });
        }
    }
}
