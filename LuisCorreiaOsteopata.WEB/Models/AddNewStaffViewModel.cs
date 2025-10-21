using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Models;

public class AddNewStaffViewModel
{
    [Required(ErrorMessage = "*Campo obrigatório")]
    [Display(Name = "First Name")]
    public string Names { get; set; }


    [Required(ErrorMessage = "*Campo obrigatório")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; }


    [Required(ErrorMessage = "*Campo obrigatório")]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; }


    [Required(ErrorMessage = "*Campo obrigatório")]
    [Phone]
    [Display(Name = "Telefone")]
    public string PhoneNumber { get; set; }

    [Required(ErrorMessage = "*Campo obrigatório")]
    [Display(Name = "Data de Nascimento")]
    public DateOnly Birthdate { get; set; }


    [Required(ErrorMessage = "*Campo obrigatório")]
    [StringLength(9)]
    public string Nif { get; set; }


    [Required(ErrorMessage = "*Campo obrigatório")]
    public string Role { get; set; } = "Colaborador";
}
