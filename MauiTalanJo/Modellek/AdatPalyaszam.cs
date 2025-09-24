using System.ComponentModel.DataAnnotations;

namespace KmKiolvasasMaui.Models
{
    public class Palyaszam
    {
        [Key]
        public int PalyaszamId { get; set; }   // pl. 4712
        public string Telephely { get; set; } = string.Empty;
    }
}
