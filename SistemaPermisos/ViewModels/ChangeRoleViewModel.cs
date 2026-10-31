using System.ComponentModel.DataAnnotations;

namespace SistemaPermisos.ViewModels
{
    public class ChangeRoleViewModel
    {
        public int UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nuevo rol es obligatorio")]
        [Display(Name = "Nuevo Rol")]
        public string NewRole { get; set; } = string.Empty;
    }
}
