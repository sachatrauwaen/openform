<%@ Control Language="C#" AutoEventWireup="false" Inherits="Satrabel.OpenForm.View" CodeBehind="View.ascx.cs" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>
<%@ Register TagPrefix="dnncl" Namespace="DotNetNuke.Web.Client.ClientResourceManagement" Assembly="DotNetNuke.Web.Client" %>
<dnncl:DnnJsInclude ID="DnnJsInclude1" runat="server" FilePath="~/DesktopModules/OpenContent/js/alpaca-1.5.8/lib/handlebars/handlebars.js" Priority="106" ForceProvider="DnnPageHeaderProvider" />
<dnncl:DnnJsInclude ID="DnnJsInclude2" runat="server" FilePath="~/DesktopModules/OpenContent/js/alpaca-1.5.8/alpaca/bootstrap/alpaca.js" Priority="107" ForceProvider="DnnPageHeaderProvider" />
<script src="/DesktopModules/OpenContent/js/wysihtml/wysihtml-toolbar.js"></script>
<script src="/DesktopModules/OpenContent/js/wysihtml/parser_rules/advanced.js"></script>
<script type="text/javascript" src="/DesktopModules/OpenContent/alpaca/js/fields/dnn/ImageField.js"></script>
<script type="text/javascript" src="/DesktopModules/OpenContent/alpaca/js/fields/dnn/wysihtmlField.js"></script>


<asp:Panel ID="pHelp" runat="server" Visible="false" >
    <h3>Get started</h3>
    <ol>
        <li>
            <asp:Label ID="scriptListLabel" runat="server" Text="Get a template > " />
            <asp:HyperLink ID="hlTempleteExchange" runat="server" Visible="false">Template Exchange</asp:HyperLink>
        </li>
         <li>
            <asp:Label ID="Label3" runat="server" Text="Chose a template > " />
            <asp:DropDownList ID="scriptList" runat="server" Visible="false" AutoPostBack="true" OnSelectedIndexChanged="scriptList_SelectedIndexChanged" />
        </li>
        <li>
            <asp:Label ID="Label1" runat="server" Text="Define settings > " />
            <asp:HyperLink ID="hlEditSettings" runat="server" Visible="false">Template Settings</asp:HyperLink>
        </li>
       
    </ol>
</asp:Panel>

<asp:Panel ID="ScopeWrapper" runat="server" EnableViewState="false">
    <div id="OpenForm">
        <div id="field1" class="alpaca"></div>
        <ul class="dnnActions dnnClear" style="display: block; padding-left: 35%">
            <li>
                <asp:HyperLink ID="cmdSave" runat="server" class="btn btn-primary" resourcekey="cmdSave" /></li>
        </ul>
    </div>
    <span id="ResultMessage"></span>
    <div id="ResultTracking"></div>
</asp:Panel>

<script type="text/javascript">
    $(document).ready(function () {
        $.alpaca.setDefaultLocale("<%= CurrentCulture %>");
        var moduleScope = $('#<%=ScopeWrapper.ClientID %>'),
            self = moduleScope,
            sf = $.ServicesFramework(<%=ModuleId %>);

        if (moduleScope.length == 0) return;
        var postData = {};
        var getData = "";
        var action = "Form";
        $.ajax({
            type: "GET",
            url: sf.getServiceRoot('OpenForm') + "OpenFormAPI/" + action,
            data: getData,
            beforeSend: sf.setModuleHeaders
        }).done(function (config) {
            var ConnectorClass = Alpaca.getConnectorClass("default");
            connector = new ConnectorClass("default");
            connector.servicesFramework = sf;

            var view = config.view;
            if (view) {
                view.parent = "bootstrap-create";
            } else {
                view = "bootstrap-create";
            }

            $("#field1", moduleScope).alpaca({
                "schema": config.schema,
                "options": config.options,
                "data": config.data,
                "view": view,
                "connector": connector,
                "postRender": function (control) {
                    var selfControl = control;
                    $("#<%=cmdSave.ClientID%>", moduleScope).click(function () {
                        selfControl.refreshValidationState(true);
                        if (selfControl.isValid(true)) {
                            var value = selfControl.getValue();
                            $(this).prop('disabled', true);
                            //alert(JSON.stringify(value, null, "  "));
                            var href = $(this).attr('href');
                            self.FormSubmit(value, href);
                            $(document).trigger("postSubmit.openform", [value, <%=ModuleId %>, sf]);
                        }
                        return false;
                    });
                    $(document).trigger("postRender.openform", [control, <%=ModuleId %>, sf ]);
                }
            });
        }).fail(function (xhr, result, status) {
            //alert("Uh-oh, something broke: " + status);
        });

        self.FormSubmit = function (data, href) {
            var postData = data;
            var action = "Submit";
            $.ajax({
                type: "POST",
                url: sf.getServiceRoot('OpenForm') + "OpenFormAPI/" + action,
                data: postData,
                beforeSend: sf.setModuleHeaders
            }).done(function (data) {
                $('#OpenForm', moduleScope).hide();
                $('#ResultMessage', moduleScope).html(data.Message);
                $('#ResultTracking', moduleScope).html(data.Tracking);

                if (data.Errors && data.Errors.length > 0) {
                    console.log(data.Errors);
                }
            }).fail(function (xhr, result, status) {
                alert("Uh-oh, something broke: " + status);
            });
        };
    });
</script>
