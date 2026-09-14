using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentShare
{
    [Key]
    public int DocumentShareId { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public DocumentShareAccess AccessLevel { get; set; } = DocumentShareAccess.Read;

    public DateTime SharedDate { get; set; } = DateTime.UtcNow;

    public DateTime? RevokedDate { get; set; }

    [ForeignKey("DocumentId")]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}

public enum DocumentShareAccess
{
    Read,
    Edit,
    Owner
}
