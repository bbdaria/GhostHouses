using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServer.Models;

public class Street
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int StreetId { get; set; }

    [Required]
    [Column(TypeName = "text")]
    public string Name { get; set; } = string.Empty;

    public ICollection<Building> Buildings { get; set; } = new List<Building>();
}
