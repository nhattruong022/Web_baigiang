namespace Lecture_web.Models.ViewModels
{
    using System.ComponentModel.DataAnnotations;

    public class BaiViewModel
    {
        public int IdBai { get; set; }
        [MaxLength(100, ErrorMessage = "Tên bài tối đa 100 ký tự")]
        public string TenBai { get; set; }

        public string NoiDung { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }

        public int IdChuong {  get; set; }
    }
}
