using System.ComponentModel.DataAnnotations;

public class ChangeEmailViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "New Email")]
    public string NewEmail { get; set; }
}
