using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Hosting;

namespace Lecture_web.Service
{
    public class ImageDataHandle
    {
        private readonly IWebHostEnvironment _env;

        public ImageDataHandle(IWebHostEnvironment env)
        {
            _env = env;
        }


        public async Task<string> ProcessImagesAsync(
            string NoiDung,
            int userId,
            int idChuong,     
            int idbai,
            bool status
        )
        {
            var userFolder = Path.Combine(_env.WebRootPath, "images", userId.ToString());
            Directory.CreateDirectory(userFolder);

            string prefix = $"{idChuong}_{idbai}_";  
            var allFiles = Directory.GetFiles(userFolder, $"{prefix}*")
                                    .Select(Path.GetFileName)
                                    .ToList();

            var keepFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var doc = new HtmlDocument();
            doc.LoadHtml(NoiDung);

            // Ảnh đã đã tồn tại, được lưu vào hệ thống trước đó
            var existingImgs = doc.DocumentNode
                .SelectNodes("//img[starts-with(@src,'/images/')]");
            if (existingImgs != null)
            {
                foreach (var img in existingImgs)
                {
                    var fileName = Path.GetFileName(img.GetAttributeValue("src", ""));
                    if (fileName.StartsWith(prefix))
                        keepFiles.Add(fileName);
                }
            }

            // Ảnh paste từ word ra, xử lý xóa base64, hash nội dung dùng md5
            var imgNodes = doc.DocumentNode
                .SelectNodes("//img[starts-with(@src,'data:image')]");
            if (imgNodes != null)
            {
                foreach (var img in imgNodes)
                {
                    var src = img.GetAttributeValue("src", "");
                    var parts = src.Split(new[] { "base64," }, StringSplitOptions.None);
                    if (parts.Length != 2) continue;
                    var mime = parts[0].Substring(5).TrimEnd(';');   
                    var ext = mime.Split('/')[1];                  

                    var bytes = Convert.FromBase64String(parts[1]);
                    // hash nội dung
                    string hash;
                    using (var md5 = MD5.Create())
                        hash = BitConverter.ToString(md5.ComputeHash(bytes))
                                         .Replace("-", "").ToLowerInvariant();

                    var fileName = $"{idChuong}_{idbai}_{hash}.{ext}";
                    var path = Path.Combine(userFolder, fileName);
                    if (!File.Exists(path))
                        await File.WriteAllBytesAsync(path, bytes);

                    img.SetAttributeValue("src", $"/images/{userId}/{fileName}");
                    keepFiles.Add(fileName);
                }
            }
            if (status)
            {
                foreach (var f in allFiles)
                {
                    if (!keepFiles.Contains(f))
                        File.Delete(Path.Combine(userFolder, f));
                }
            }

            return doc.DocumentNode.InnerHtml;
        }


    }
}