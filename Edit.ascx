<%@ Control Language="C#" AutoEventWireup="True" CodeBehind="Edit.ascx.cs" Inherits="Satrabel.OpenForm.Edit" %>
<%@ Import Namespace="DotNetNuke.Services.Localization" %>
<%@ Register TagPrefix="dnn" TagName="label" Src="~/controls/LabelControl.ascx" %>
<div class="dnnForm dnnEditBasicSettings" id="dnnEditBasicSettings">
    <fieldset>
        <div class="dnnFormItem">
            <asp:GridView ID="gvData" runat="server" CssClass="dnnGrid" GridLines="None" 
                AutoGenerateColumns="true" Width="98%" 
                EnableViewState="false" BorderStyle="None" >
                <HeaderStyle CssClass="dnnGridHeader" VerticalAlign="Top" />
                <RowStyle CssClass="dnnGridItem" HorizontalAlign="Left" />
                <AlternatingRowStyle CssClass="dnnGridAltItem" />
                <FooterStyle CssClass="dnnGridFooter" />
                <PagerStyle CssClass="dnnGridPager" />
                
            </asp:GridView>
        </div>
    </fieldset>
    <p><a href="#" runat="server" id="excelDownload" >Download as Excel</a></p>
</div>

<script type="text/javascript">
    /*globals jQuery, window, Sys */
    (function ($, Sys) {
        function dnnEditBasicSettings() {
            //$('#dnnEditBasicSettings').dnnPanels();
            //$('#dnnEditBasicSettings .dnnFormExpandContent a').dnnExpandAll({ expandText: '<%=Localization.GetString("ExpandAll", LocalResourceFile)%>', collapseText: '<%=Localization.GetString("CollapseAll", LocalResourceFile)%>', targetArea: '#dnnEditBasicSettings' });
        }

        $(document).ready(function () {
            dnnEditBasicSettings();
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                dnnEditBasicSettings();
            });
        });

    }(jQuery, window.Sys));
</script>
