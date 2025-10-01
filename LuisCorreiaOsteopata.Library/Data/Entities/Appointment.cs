using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.Library.Data.Entities;

public class Appointment : IEntity
{
    public int Id { get; set; }


    [Required]
    public Patient Patient { get; set; }


    [Required]
    public Staff Staff { get; set; }


    [Required]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
    [Display(Name = "Created Date")]
    public DateTime CreatedDate => DateTime.Now;


    [Required]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
    [Display(Name = "Appointment Date")]
    public DateTime AppointmentDate { get; set; }


    public DateTime? AppointmentDateLocal => this.AppointmentDate == null ? null : this.AppointmentDate.ToLocalTime();


    [Required]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy hh:mm}", ApplyFormatInEditMode = false)]
    [Display(Name = "Start Time")]
    public DateTime StartTime { get; set; }


    [Required]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy hh:mm}", ApplyFormatInEditMode = false)]
    [Display(Name = "End Time")]
    public DateTime EndTime { get; set; }


    [MaxLength(500)]
    public string? Notes { get; set; }


    [Required]
    public AppointmentStatus Status => AppointmentStatus.Booked;

    //TODO: Corrigir o delete em cascata porque a base de dados não está a ser criada
}


public enum AppointmentStatus
{
    Booked,
    Completed,
    Cancelled
}
