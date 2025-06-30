using System.ComponentModel.DataAnnotations;

namespace Lecture_web.Models.ViewModels
{
    public class CreateBaiViewModel
    {
        [Required(ErrorMessage = "Chưa nhập tiêu đề bài")]
        [StringLength(100, ErrorMessage = "Tiêu đề không được vượt quá 100 ký tự")]
        public string TieuDeBai { get; set; }

        [Required(ErrorMessage = "Chưa nhập nội dung bài")]
        public string NoiDung { get; set; }

        [Required]
        public int IdChuong { get; set; }

        public int IdBaiGiang { get; set; }
    }

}
