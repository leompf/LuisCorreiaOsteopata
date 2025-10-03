using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;


namespace LuisCorreiaOsteopata.WEB.Models;

public class BookAppointmentViewModel
{
    [Required]
    [Display(Name = "Appointment Date")]
    [DisplayFormat(DataFormatString = "{0:MM-dd-yyyy}", ApplyFormatInEditMode = false)]
    public DateTime AppointmentDate { get; set; } = DateTime.Today;


    [Required(ErrorMessage = "Selecione uma hora")]
    [Display(Name = "Start Time")]
    public string StartTime { get; set; }

    
    public string? Notes { get; set; }


    [Required]
    [Display(Name = "Staff")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecione um profissional")]
    public int StaffId { get; set; }


    [ValidateNever]
    public IEnumerable<SelectListItem> Staff { get; set; }


    [ValidateNever]
    public IEnumerable<SelectListItem> TimeSlots { get; set; }
}
 