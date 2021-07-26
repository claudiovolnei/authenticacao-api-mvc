using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuizaLabs.WebApi.Models
{
    public class Usuario
    {
        public Usuario(string nome, string email, string senha)
        {
            Nome = nome;
            Email = email;
            Senha = senha;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; private set; }
        [Display(Name = "Nome")]
        public string Nome { get; private set; }
        [Display(Name = "E-mail")]
        public string Email { get; private set; }
        [Display(Name = "Senha")]
        public string Senha { get; private set; }
        [NotMapped]
        public string Token { get; set; }
    }
}