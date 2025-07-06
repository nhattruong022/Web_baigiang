using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OpenXmlPowerTools;

namespace Lecture_web.Service
{
    public class ConvertWordToHTML
    {
        public async Task<string> ConvertAsync(Stream docxStream)
        {
            // 1) Đọc vào MemoryStream để có thể mở lại nhiều lần nếu cần
            using var mem = new MemoryStream();
            await docxStream.CopyToAsync(mem);
            mem.Position = 0;

            // 2) Mở WordprocessingDocument để đọc & ghi
            using var wordDoc = WordprocessingDocument.Open(mem, true);
            var mainPart = wordDoc.MainDocumentPart;
            var body = mainPart.Document.Body;

            // Namespace để tìm thẻ <w:hyperlink>
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            // 3) Duyệt tất cả hyperlink trong Body
            foreach (var h in body.Descendants<Hyperlink>().ToList())
            {
                // Lấy URI hiện tại
                string uri = null;
                if (h.Id != null)
                {
                    var rel = mainPart.HyperlinkRelationships.FirstOrDefault(r => r.Id == h.Id);
                    uri = rel?.Uri.ToString();
                }
                else if (!string.IsNullOrEmpty(h.Anchor))
                {
                    uri = h.Anchor;
                }


                bool isValid = !string.IsNullOrWhiteSpace(uri)
                               && Uri.IsWellFormedUriString(uri, UriKind.Absolute);

                if (!isValid && h.Id != null)
                {
                    // Xóa relationship cũ
                    mainPart.DeleteReferenceRelationship(h.Id);
                    mainPart.AddHyperlinkRelationship(
                        new Uri("#", UriKind.Relative),
                        false,
                        h.Id);
                }
            }

            // Lưu document sau khi sửa hyperlink
            mainPart.Document.Save();

            // 4) Cấu hình HtmlConverter để xuất HTML với inline CSS và ảnh base64
            var settings = new HtmlConverterSettings
            {
                FabricateCssClasses = true,
                CssClassPrefix = "pt-",
                PageTitle = "Converted Document",
                ImageHandler = imageInfo =>
                {
                    using var msImg = new MemoryStream();
                    imageInfo.Bitmap.Save(msImg, System.Drawing.Imaging.ImageFormat.Png);
                    var base64 = Convert.ToBase64String(msImg.ToArray());
                    return new XElement(Xhtml.img,
                        new XAttribute(NoNamespace.src, $"data:image/png;base64,{base64}"),
                        imageInfo.ImgStyleAttribute,
                        imageInfo.AltText != null
                          ? new XAttribute(NoNamespace.alt, imageInfo.AltText)
                          : null);
                }
            };

            // 5) Thực thi chuyển đổi
            XElement html = HtmlConverter.ConvertToHtml(wordDoc, settings);

            // 6) Trả HTML dưới dạng string
            return html.ToString(SaveOptions.DisableFormatting);
        }
    }
}
