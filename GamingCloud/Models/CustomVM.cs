using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GamingCloud.Models;

public class CustomVM
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AutoID { get; set; }
    
    [StringLength(50, MinimumLength = 3)]
    public string name { get; set; }
    public string IP { get; set; }
    
    [Required]
    public bool active { get; set; }
    
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string login { get; set; }
    
    [Required]
    [DataType(DataType.Password)]
    [StringLength(50, MinimumLength = 3)]
    public string password { get; set; }

    public bool IsValid()
    {
        return login.Length > 1 && password.Length > 1;
    }
    
}