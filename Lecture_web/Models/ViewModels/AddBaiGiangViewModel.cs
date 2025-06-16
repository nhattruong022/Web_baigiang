using System.ComponentModel.DataAnnotations;

namespace Lecture_web.Models.ViewModels
{
    public class AddBaiGiangViewModel
    {

        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [StringLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự")]
        public string tieude { get; set; }

        [Required(ErrorMessage = "Mô tả không được để trống")]
        [StringLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự")]
        public string mota { get; set; }

        [Required(ErrorMessage = "Phải chọn ít nhất một lớp học phần")]
        public List<int> selectedlophocphanids { get; set; } = new();
    }
}
