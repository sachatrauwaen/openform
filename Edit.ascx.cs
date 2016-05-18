/*
' Copyright (c) 2015  Satrabel.com
'  All rights reserved.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
' TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
' THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
' CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
' DEALINGS IN THE SOFTWARE.
' 
*/
using System;
using System.Linq;
using DotNetNuke.Entities.Users;
using Satrabel.OpenForm.Components;
using DotNetNuke.Services.Exceptions;
using Satrabel.OpenContent.Components.Json;
using System.Collections.Generic;
using System.Dynamic;
using System.Web.Helpers;
using System.Data;
using System.ComponentModel;

namespace Satrabel.OpenForm
{
    public partial class Edit : OpenFormModuleBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                if (!Page.IsPostBack)
                {
                    OpenFormController ctrl = new OpenFormController();
                    var data = ctrl.GetContents(ModuleId).OrderByDescending(c=> c.CreatedOnDate);
                    var dynData = new List<dynamic>();
                    foreach (var item in data)
                    {
                        dynamic o = new ExpandoObject();
                        var dict = (IDictionary<string, object>)o;
                        o.CreatedOnDate = item.CreatedOnDate;
                        //o.Json = item.Json;
                        dynamic d = JsonUtils.JsonToDynamic(item.Json);
                        //o.Data = d;
                        Dictionary<String, Object> jdic = Dyn2Dict(d);
                        foreach (var p in jdic)
                        {
                            dict[p.Key] = p.Value;
                        }
                        dynData.Add(o);
                    }
                    gvData.DataSource = ToDataTable(dynData);
                    gvData.DataBind();
                }
            }
            catch (Exception exc) //Module failed to load
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }
        public Dictionary<String, Object> Dyn2Dict(dynamic dynObj)
        {
            var dictionary = new Dictionary<string, object>();
            foreach (var name in dynObj.GetDynamicMemberNames())
            {
                dictionary.Add(name, GetProperty(dynObj, name));
            }
            return dictionary;
        }

        private static object GetProperty(object target, string name)
        {
            var site = System.Runtime.CompilerServices.CallSite<Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(0, name, target.GetType(), new[] { Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(0, null) }));
            return site.Target(site, target);
        }

        public static DataTable ToDataTable(IEnumerable<dynamic> items)
        {
            var data = items.ToArray();
            if (data.Count() == 0) return null;
            var dt = new DataTable();
            foreach (var key in ((IDictionary<string, object>)data[0]).Keys)
            {
                dt.Columns.Add(key);
            }
            foreach (var d in data)
            {
                List<object> row = new List<object>();
                var dic = (IDictionary<string, object>)d;
                foreach (var key in ((IDictionary<string, object>)data[0]).Keys)
                {
                    if (dic.ContainsKey(key))
                        row.Add(dic[key]);
                }
                dt.Rows.Add(row.ToArray());
            }
            return dt;
        }
       
    }
}