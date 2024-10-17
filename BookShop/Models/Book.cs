namespace BookShop.Models
{
    public class Book
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public string CoverImageUrl { get; set; }

        public int AuthorId { get; set; }
        public Author Author { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public int Stock { get; set; }

        public string Description { get; set; }
        public string ISBN { get; set; }
        public int PublicationYear { get; set; }
        public string Language { get; set; }
        public float Weight { get; set; }

        public ICollection<Review> Reviews { get; set; }
    }
}
