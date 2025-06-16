namespace Lecture_web.Models.ViewModels
{
    public class SearchBaiGiangViewModel<L>
    {
        public List<L> Items { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SearchTerm { get; set; }

    }
}
