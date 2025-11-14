// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Licenta3.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

namespace Licenta3.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _roleManager = roleManager;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [Display(Name = "Prenume")]
            public string FirstName { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [Display(Name = "Nume de familie")]
            public string LastName { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "Parolele nu sunt identice.")]
            public string ConfirmPassword { get; set; }
            public string Role { get; set; }

            public IEnumerable<SelectListItem> RolesList { get; set; }

        }


        public async System.Threading.Tasks.Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            Input = new InputModel
            {
                RolesList = _roleManager.Roles.Select(x => x.Name).Select(i => new SelectListItem
                {
                    Text = i,
                    Value = i
                })
            };
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        
            if (ModelState.IsValid)
            {
                var user = CreateUser();
                user.FirstName = Input.FirstName;
                user.LastName = Input.LastName;
        
                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
        
                var result = await _userManager.CreateAsync(user, Input.Password);
        
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");
        
                    await _userManager.AddToRoleAsync(user, Input.Role);
        
                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);
        
                    await SendEmailAsync(
                        Input.Email,
                        "Confirmare adresă de email - ProjecTrack",
                        $@"
                        <div style='font-family:Segoe UI,Roboto,Helvetica,Arial,sans-serif;font-size:15px;color:#333'>
                            <h2 style='color:#1a73e8'>Bun venit la ProjecTrack!</h2>
                            <p>Salut <strong>{Input.FirstName}</strong>,</p>
                            <p>
                                Pentru a finaliza procesul de înregistrare, te rugăm să confirmi adresa ta de email
                                accesând linkul de mai jos:
                            </p>
                            <p style='margin:30px 0'>
                                <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' 
                                   style='background-color:#1a73e8;color:white;padding:10px 20px;text-decoration:none;border-radius:6px'>
                                    Confirmă adresa de email
                                </a>
                            </p>
                            <br/>
                            <p>Cu stimă,<br/><strong>Echipa ProjecTrack</strong></p>
                        </div>"
                    );
        
                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
        
                var passwordErrors = new List<string>();
        
                foreach (var error in result.Errors)
                {
                    switch (error.Code)
                    {
                        case "PasswordRequiresNonAlphanumeric":
                            passwordErrors.Add("Parola trebuie să conțină cel puțin un caracter special.");
                            break;
        
                        case "PasswordRequiresUpper":
                            passwordErrors.Add("Parola trebuie să conțină cel puțin o literă mare (A–Z).");
                            break;
        
                        case "PasswordRequiresLower":
                            passwordErrors.Add("Parola trebuie să conțină cel puțin o literă mică (a–z).");
                            break;
        
                        case "PasswordRequiresDigit":
                            passwordErrors.Add("Parola trebuie să conțină cel puțin o cifră (0–9).");
                            break;
        
                        case "PasswordTooShort":
                            passwordErrors.Add("Parola trebuie să aibă cel puțin 6 caractere.");
                            break;
        
                        default:
                            ModelState.AddModelError(string.Empty, error.Description);
                            break;
                    }
                }
        
                if (passwordErrors.Any())
                {
                    ModelState.AddModelError("Input.Password",
                        "<ul><li>" + string.Join("</li><li>", passwordErrors) + "</li></ul>");
                }
            }
        
            Input.RolesList = _roleManager.Roles
                .Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Name
                });
        
            return Page();
        }

        private async Task<bool> SendEmailAsync(string email, string subject, string htmlContent)
        {
            try
            {
                var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
                var fromEmail = Environment.GetEnvironmentVariable("SENDGRID_FROM_EMAIL");
                if (string.IsNullOrEmpty(apiKey))
                    throw new Exception("Missing Key (SENDGRID_API_KEY)!");

                var client = new SendGridClient(apiKey);

                var from = new EmailAddress(fromEmail, "ProjecTrack");
                var to = new EmailAddress(email);

                var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);

                var response = await client.SendEmailAsync(msg);

                return response.StatusCode == System.Net.HttpStatusCode.Accepted;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare la trimiterea mailului: {ex.Message}");
                return false;
            }
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
