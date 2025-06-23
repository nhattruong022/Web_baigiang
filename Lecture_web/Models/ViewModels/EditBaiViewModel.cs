using System.ComponentModel.DataAnnotations;

namespace Lecture_web.Models.ViewModels
{
    public class EditBaiViewModel
    {

        public int IdBai { get; set; }

        [Required(ErrorMessage = "Chưa nhập tiêu đề bài")]
        [Display(Name = "Tiêu đề bài")]
        public string TieuDeBai { get; set; }

        [Display(Name = "Nội dung")]
        public string NoiDung { get; set; }

        [Required(ErrorMessage = "Chưa chọn chương")]
        [Display(Name = "Chọn chương")]
        public int IdChuong { get; set; }
    }
}
