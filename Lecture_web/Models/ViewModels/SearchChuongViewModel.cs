namespace Lecture_web.Models.ViewModels
{
    public class SearchChuongViewModel<C>
    {
        public List<C> Items { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SearchTerm { get; set; }

        public string baigiang {  get; set; }
    }
}
