using Microsoft.AspNetCore.Identity;

namespace BookShop.Models
{
    public class Review
    {
        public int ReviewId { get; set; }
        public string UserId { get; set; }
        public virtual IdentityUser User { get; set; }
        public int BookId { get; set; }
        public string Content { get; set; }
        public int Rating { get; set; }

        public Book Book { get; set; }
    }

}
