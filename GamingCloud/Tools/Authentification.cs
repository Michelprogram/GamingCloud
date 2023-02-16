using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GamingCloud.Tools;

public class Authentification : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        // Vérifier si l'utilisateur est authentifié
        if (!user.Identity.IsAuthenticated)
        {
            // L'utilisateur n'est pas connecté, rediriger vers la page de connexion
            context.Result = new RedirectToRouteResult(new { controller = "Home", action = "Login" });
        }
    }
}