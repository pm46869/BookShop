using System.ComponentModel.DataAnnotations;

public class ChangeUsernameViewModel
{
    [Required]
    [Display(Name = "New Username")]
    public string NewUsername { get; set; }
}