using System.ComponentModel.DataAnnotations;

namespace PRJ_Panaderia.Models;

public class ConfiguracionSistema
{
    public int ConfigID { get; set; } = 1;

    [Required(ErrorMessage = "El nombre del negocio es obligatorio.")]
    [StringLength(80)]
    [Display(Name = "Nombre Negocio")]
    public string NombreNegocio { get; set; } = string.Empty;

    [Required(ErrorMessage = "El RUC es obligatorio.")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "El RUC debe tener 11 dígitos.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "El RUC debe contener solo números.")]
    [Display(Name = "RUC")]
    public string RUC { get; set; } = string.Empty;

    [Required(ErrorMessage = "La razón social es obligatoria.")]
    [StringLength(120)]
    [Display(Name = "Razón Social")]
    public string RazonSocial { get; set; } = string.Empty;

    [Required(ErrorMessage = "El IGV es obligatorio.")]
    [Range(0, 100, ErrorMessage = "El IGV debe estar entre 0 y 100.")]
    [Display(Name = "IGV %")]
    public decimal IGV_Porcentaje { get; set; } = 18.00m;

    [Required(ErrorMessage = "La moneda es obligatoria.")]
    [Display(Name = "Moneda")]
    public string Moneda { get; set; } = "PEN";

    [StringLength(15)]
    [Display(Name = "Número Yape")]
    public string? NumeroYape { get; set; }

    [Required(ErrorMessage = "El correo del sistema es obligatorio.")]
    [RegularExpression(@"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$", ErrorMessage = "El correo no tiene un formato válido. Ejemplo: ventas@negocio.com")]
    [StringLength(100)]
    [Display(Name = "Correo del Sistema")]
    public string Correo { get; set; } = string.Empty;
}
