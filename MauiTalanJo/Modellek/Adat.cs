using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KmKiolvasasMaui.Models
{
    public class Adat
    {
        [Key]
        public int Id { get; set; }

        public DateTime Datum { get; set; }
        public int Napi_km { get; set; }
        public int Ossz_km { get; set; }
        public bool Email_kuldve { get; set; }

        [ForeignKey(nameof(Palyaszam))]
        public int PalyaszamId { get; set; }

        public Palyaszam? Palyaszam { get; set; }  // kapcsolat
    }
}
