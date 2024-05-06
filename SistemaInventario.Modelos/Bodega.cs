using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaInventario.Modelos
{
    public class Bodega
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Nombre requerido")]
        [MaxLength(60, ErrorMessage = "Nombre de máximo 60 catracteres")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Descripción requerido")]
        [MaxLength(100, ErrorMessage = "Descripción de máximo 100 catracteres")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "Estado requerido")]
        public bool Estado { get; set;}
    }
}
