namespace IdentityServerHost.Models;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    [Required]
    [PersonalData]
    public string FirstName { get; set; }

    [Required]
    [PersonalData]
    public string LastName { get; set; }
}
