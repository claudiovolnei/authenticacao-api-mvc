﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LuizaLabs.App.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using LuizaLabs.App.ValuesObjects;
using System.Security.Claims;
using LuizaLabs.App.Services.Interface;
using LuizaLabs.App.Util;
using Coravel.Mailer.Mail.Interfaces;
using LuizaLabs.App.Services;
using LuizaLabs.App.ViewModels;

namespace LuizaLabs.App.Controllers
{
    public class AccountController : Controller
    {
        private readonly IMailer _mailer;
        private readonly ILogger<AccountController> _logger;
        private readonly IAccountService _accountService;

        public AccountController(ILogger<AccountController> logger, IAccountService accountService, IMailer mailer)
        {
            _mailer = mailer;
            _accountService = accountService;
            _logger = logger;
        }
        public IActionResult Login()
        {
            return View();
        }
        public IActionResult RecuperarSenha()
        {
            return View();
        }

        public async Task<IActionResult> AlterarSenha(Guid id)
        {
            var result = await _accountService.RecuperaSenhaId(id);
            if (!result.Status)
            {
                ViewBag.Result = "Recuperação de senha expirou ou esta invalida!";
                return View();
            }

            return View(result.Dados);
        }
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Usuario usuario)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var result = await _accountService.Registrar(usuario);

                    if (!result.Status)
                    {
                        ModelState.AddModelError(string.Empty, result.Mensagem);
                        return View("Register");
                    }
                    else
                    {
                        await this._mailer.SendAsync(new SendEmail(result));
                        ViewBag.Mensagem = result.Mensagem;
                        return RedirectToAction("Login");
                    }
                }
                else
                {
                    var errors = from modelstate in ModelState.AsQueryable().Where(f => f.Value.Errors.Count > 0) select new { Title = modelstate.Key };
                    ModelState.AddModelError(string.Empty, errors.ToString());
                    return View("Register");
                }
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("Register");
            }

        }
        [HttpPost]
        public async Task<IActionResult> AlterarSenha(UsuarioAlteracaoSenha usuarioAlteracaoSenha)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var result = await _accountService.EnviarSenhaNova(usuarioAlteracaoSenha);

                    if (!result.Status)
                    {
                        ModelState.AddModelError(string.Empty, result.Mensagem);
                        return View("AlterarSenha");
                    }
                    else
                    {
                        ViewBag.Result = result.Mensagem;
                        return View("AlterarSenha");
                    }
                }
                else
                {
                    string erros = string.Join("<br>", ModelState.Values.Where(e => e.Errors.Count > 0)
                                                                            .SelectMany(e => e.Errors)
                                                                            .Select(e => e.ErrorMessage)
                                                                            .ToArray());
                    ModelState.AddModelError(string.Empty, erros);
                    return View("AlterarSenha");
                }
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("AlterarSenha");
            }

        }

        [HttpPost]
        public async Task<IActionResult> Logar(UsuarioLogin usuario)
        {
            if (ModelState.IsValid)
            {
                var result = await _accountService.Autenticar(usuario);

                if (!result.Status)
                {
                    ModelState.AddModelError(string.Empty, result.Mensagem);
                    return View("Login");
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, result.Dados.Email),
                    new Claim("Nome", result.Dados.Nome),
                    new Claim(ClaimTypes.Role, "Administrator"),
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    //AllowRefresh = <bool>,
                    // Refreshing the authentication session should be allowed.

                    //ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                    // The time at which the authentication ticket expires. A 
                    // value set here overrides the ExpireTimeSpan option of 
                    // CookieAuthenticationOptions set with AddCookie.

                    //IsPersistent = true,
                    // Whether the authentication session is persisted across 
                    // multiple requests. When used with cookies, controls
                    // whether the cookie's lifetime is absolute (matching the
                    // lifetime of the authentication ticket) or session-based.

                    //IssuedUtc = <DateTimeOffset>,
                    // The time at which the authentication ticket was issued.

                    //RedirectUri = <string>
                    // The full path or absolute URI to be used as an http 
                    // redirect response value.
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("User {Email} logged in at {Time}.",
                    result.Dados.Email, DateTime.UtcNow);

                return RedirectToAction("Index", "Home");
            }
            else
            {
                string erros = string.Join("<br>", ModelState.Values.Where(e => e.Errors.Count > 0)
                                                                            .SelectMany(e => e.Errors)
                                                                            .Select(e => e.ErrorMessage)
                                                                            .ToArray());
                ModelState.AddModelError(string.Empty, erros);
                return View("Login");
            }
        }

        [HttpPost]
        public async Task<IActionResult> RecuperarSenha(UsuarioRecuperacaoSenha usuarioRecuperacaoSenha)
        {
            if (ModelState.IsValid)
            {
                var result = await _accountService.RecuperarSenha(usuarioRecuperacaoSenha);

                if (!result.Status)
                {
                    ModelState.AddModelError(string.Empty, result.Mensagem);
                    return View("RecuperarSenha");
                }
                else
                {
                    await this._mailer.SendAsync(new SendEmail(result));
                    ViewBag.Result = "Confirmação de senha enviado por e-mail.";
                    return View("RecuperarSenha");
                }
            }
            else
            {
                string erros = string.Join("<br>", ModelState.Values.Where(e => e.Errors.Count > 0)
                                                                            .SelectMany(e => e.Errors)
                                                                            .Select(e => e.ErrorMessage)
                                                                            .ToArray());
                ModelState.AddModelError(string.Empty, erros);
                return View("Login");
            }
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login");
        }
    }
}
