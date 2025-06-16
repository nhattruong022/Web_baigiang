using System.ComponentModel.DataAnnotations;

namespace Lecture_web.Models.ViewModels
{
    public class LuuBaiGiangViewModel
    {
        public int idbaigiang { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [MaxLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự")]
       
        public string tieude { get; set; }

        [Required(ErrorMessage = "Mô tả không được để trống")]
        [MaxLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự")]
        public string mota { get; set; }
        public List<int>?  selectlophoc { get; set; }
    }
}
