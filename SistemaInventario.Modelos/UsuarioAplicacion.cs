using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaInventario.Modelos
{
    public class UsuarioAplicacion : IdentityUser
    {
        [Required(ErrorMessage ="El nombre es requerido")]
        [MaxLength(80)]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "El apellido es requerido")]
        [MaxLength(80)]
        public string Apellideos { get; set; }

        [Required(ErrorMessage = "La dirección es requerida")]
        [MaxLength(200)]
        public string Direccion { get; set; }

        [Required(ErrorMessage = "La ciudad es requerida")]
        [MaxLength(60)]
        public string Ciudad { get; set; }

        [Required(ErrorMessage = "El pais es requerido")]
        [MaxLength(60)]
        public string Pais { get; set; }

        [NotMapped] //No se agrega a la base de datos, pero existe en el modelo
        public string Role { get; set; }
    }
}
