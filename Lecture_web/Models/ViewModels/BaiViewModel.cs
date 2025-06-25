namespace Lecture_web.Models.ViewModels
{
    public class BaiViewModel
    {
        public int IdBai { get; set; }
        public string TenBai { get; set; }

        public string NoiDung { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }

        public int IdChuong {  get; set; }
    }
}
