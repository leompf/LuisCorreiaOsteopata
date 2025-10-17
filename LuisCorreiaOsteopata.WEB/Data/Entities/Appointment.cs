using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Data.Entities;

public class Appointment : IEntity
{
    public int Id { get; set; }


    [Required]
    public int PatientId { get; set; }
    public Patient Patient { get; set; }


    [Required]
    public int StaffId { get; set; }
    public Staff Staff { get; set; }


    [Required]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
    [Display(Name = "Appointment Date")]
    public DateTime AppointmentDate { get; set; }


    [Required]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy hh:mm}", ApplyFormatInEditMode = false)]
    [Display(Name = "Start Time")]
    public DateTime StartTime { get; set; }


    [Required]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy hh:mm}", ApplyFormatInEditMode = false)]
    [Display(Name = "End Time")]
    public DateTime EndTime { get; set; }


    [MaxLength(1000)]
    public string? PatientNotes { get; set; }


    public string? StaffNotes { get; set; }


    [Required]
    public string AppointmentStatus {  get; set; }


    public int? OrderDetailId { get; set; }
    public OrderDetail? OrderDetail { get; set; }

    [Required]
    public bool IsPaid { get; set; }
}

