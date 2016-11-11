using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WF_GestionNC.Models;

namespace WF_GestionNC.Controllers
{
    [HandleError]
    public class HomeController : Controller
    {
        WF_GestionNCDataContext contexto = new WF_GestionNCDataContext();
        
        public ActionResult Index(string Login)
        {
            ViewData["Message"] = "Welcome to ASP.NET MVC!";
            if (Login == "" || string.IsNullOrEmpty(Login))
            {
                this.login();                
            }
            else
            {
                WF_GestionNC.Models.WF_GestionNCDataContext contexto = new WF_GestionNC.Models.WF_GestionNCDataContext();
                foreach (var usuario in contexto.Usuarios.Where(item => item.GID == Login))
                {
                    String[] parametros = new String[] { usuario.GID, 
                                                             usuario.PrimerApellido,
                                                             usuario.SegundoApellido,
                                                             usuario.PrimerNombre,
                                                             usuario.SegundoNombre };
                    Session["usuario"] = string.Format("{0}  {1} {2}, {3} {4}", parametros);
                    Session["usuarioGID"] = usuario.GID;
                    Session["Administrador"] = usuario.UsuariosPorGrupos.Any(grupo => grupo.CodigoGrupo == 1);
                }
            }
            return View();
        }

        public ActionResult About()
        {
            return View();
        }
        public void login()
        {
            if (Session["usuario"] == null)
            {
                string usuarioRed = string.Empty;
                string usuarioGID = string.Empty;
                if (System.Configuration.ConfigurationManager.AppSettings["PKI"] == "1")
                {
                    if (Request.ServerVariables.Get("HTTP_SCGID") != null)
                    {
                        usuarioGID = Request.ServerVariables.Get("HTTP_SCGID");
                        usuarioGID = usuarioRed.Substring(usuarioRed.IndexOf("\\") + 1, usuarioRed.Length - usuarioRed.IndexOf("\\") - 1);
                        usuarioGID = usuarioRed.ToUpper();
                    }
                }
                else
                {
                    usuarioRed = Request.ServerVariables["LOGON_USER"];
                    usuarioRed = usuarioRed.Substring(usuarioRed.IndexOf("\\") + 1, usuarioRed.Length - usuarioRed.IndexOf("\\") - 1);
                    usuarioRed = usuarioRed.ToUpper();
                }

               
                foreach (var usuario in contexto.Usuarios.Where(item => (item.GID == usuarioGID || item.UsuarioRed == usuarioRed) && (item.UsuarioRed != "")))
                {
                    String[] parametros = new String[] { usuario.GID, 
                                                     usuario.PrimerApellido,
                                                     usuario.SegundoApellido,
                                                     usuario.PrimerNombre,
                                                     usuario.SegundoNombre };

                    Session["usuario"] = string.Format("{0}  {1} {2}, {3} {4}", parametros);
                    Session["usuarioGID"] = usuario.GID;
                    Session["Administrador"] = usuario.UsuariosPorGrupos.Any(grupo => grupo.CodigoGrupo == 1);
                }
                
            }
        }
    }
}
