using System.ComponentModel.DataAnnotations;

namespace Lecture_web.Models.ViewModels
{
    public class EditChuongViewModel
    {
        public int idchuong { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [StringLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự")]
        public string tenchuong { get; set; }


        [Required]
        public int IdBaiGiang { get; set; }
    }
}
