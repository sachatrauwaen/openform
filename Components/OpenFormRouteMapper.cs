#region Copyright

// 
// Copyright (c) 2015
// by Satrabel
// 

#endregion

#region Using Statements

using DotNetNuke.Web.Api;

#endregion

namespace Satrabel.OpenForm.Components
{
    public class FormRouteMapper : IServiceRouteMapper
    {
        public void RegisterRoutes(IMapRoute mapRouteManager)
        {
            mapRouteManager.MapHttpRoute("OpenForm", "default", "{controller}/{action}", new[] { "Satrabel.OpenForm.Components" });
        }
    }
} 

