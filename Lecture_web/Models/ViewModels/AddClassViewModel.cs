using System.ComponentModel.DataAnnotations;

namespace Lecture_web.Models.ViewModels
{
    public class AddClassViewModel
    {
        [Required(ErrorMessage = "Chưa chọn học phần")]
        public int? HocPhanId { get; set; }

        [Required(ErrorMessage = "Chưa nhập tên lớp")]

        [MaxLength(50, ErrorMessage = "Tên lớp không được vượt quá 50 ký tự")]
        public string TenLop { get; set; }

        [MaxLength(100,ErrorMessage = "Mô tả không được vượt quá 100 ký tự")]
        public string? MoTa { get; set; }


        public int? BaiGiangId { get; set; }   

    }
}
