using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;
using OpenXmlPowerTools;

namespace Lecture_web.Service
{
    public class ConvertWordToHTML
    {
        public async Task<string> ConvertAsync(Stream docxStream)
        {
           //Đọc vào MemoryStream để có thể mở lại nhiều lần nếu cần
            using var mem = new MemoryStream();
            await docxStream.CopyToAsync(mem);
            mem.Position = 0;

            // Mở Word ở chế độ Read/Write
            using var wordDoc = WordprocessingDocument.Open(mem, true);

            //Cấu hình HtmlConverter để xuất HTML với inline CSS và ảnh base64
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
                            : null
                    );
                }
            };

            //convert
            XElement htmlElement = HtmlConverter.ConvertToHtml(wordDoc, settings);


            //Trả về HTML dưới dạng string (không định dạng thêm)
            return htmlElement.ToString(SaveOptions.DisableFormatting);
        }
    }
}
