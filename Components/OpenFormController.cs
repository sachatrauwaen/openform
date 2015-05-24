/*
' Copyright (c) 2015 Satrabel.be
'  All rights reserved.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
' TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
' THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
' CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
' DEALINGS IN THE SOFTWARE.
' 
*/
using System.Linq;
using System.Collections.Generic;
using DotNetNuke.Data;

namespace Satrabel.OpenForm.Components
{
    public class OpenFormController
    {
        public void AddContent(OpenFormInfo Content)
        {
            using (IDataContext ctx = DataContext.Instance())
            {
                var rep = ctx.GetRepository<OpenFormInfo>();
                rep.Insert(Content);
            }
        }

        public void DeleteContent(OpenFormInfo Content)
        {
            using (IDataContext ctx = DataContext.Instance())
            {
                var rep = ctx.GetRepository<OpenFormInfo>();
                rep.Delete(Content);
            }
        }

        public IEnumerable<OpenFormInfo> GetContents(int moduleId)
        {
            IEnumerable<OpenFormInfo> Contents;

            using (IDataContext ctx = DataContext.Instance())
            {
                var rep = ctx.GetRepository<OpenFormInfo>();
                Contents = rep.Get(moduleId);
            }
            return Contents;
        }

        public OpenFormInfo GetContent(int ContentId, int moduleId)
        {
            OpenFormInfo Content;

            using (IDataContext ctx = DataContext.Instance())
            {
                var rep = ctx.GetRepository<OpenFormInfo>();
                Content = rep.GetById(ContentId, moduleId);
            }
            return Content;
        }

        public OpenFormInfo GetFirstContent(int moduleId)
        {
            OpenFormInfo Content;

            using (IDataContext ctx = DataContext.Instance())
            {
                var rep = ctx.GetRepository<OpenFormInfo>();
                Content = rep.Get(moduleId).FirstOrDefault();
            }
            return Content;
        }

        public void UpdateContent(OpenFormInfo Content)
        {
            using (IDataContext ctx = DataContext.Instance())
            {
                var rep = ctx.GetRepository<OpenFormInfo>();
                rep.Update(Content);
            }
        }

    }
}
