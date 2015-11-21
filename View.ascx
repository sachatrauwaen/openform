<%@ Control Language="C#" AutoEventWireup="false" Inherits="Satrabel.OpenForm.View" CodeBehind="View.ascx.cs" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>
<%@ Register TagPrefix="dnncl" Namespace="DotNetNuke.Web.Client.ClientResourceManagement" Assembly="DotNetNuke.Web.Client" %>
<dnncl:DnnJsInclude ID="DnnJsInclude1" runat="server" FilePath="~/DesktopModules/OpenContent/js/alpaca-1.5.8/lib/handlebars/handlebars.js" Priority="106" ForceProvider="DnnPageHeaderProvider" />
<dnncl:DnnJsInclude ID="DnnJsInclude2" runat="server" FilePath="~/DesktopModules/OpenContent/js/alpaca-1.5.8/alpaca/bootstrap/alpaca.js" Priority="107" ForceProvider="DnnPageHeaderProvider" />

<asp:Panel ID="pHelp" runat="server" Visible="false">
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

<asp:PlaceHolder runat="server" ID="phForm">
    <asp:Panel ID="ScopeWrapper" runat="server" EnableViewState="false">
        <div id="OpenForm" class="OpenForm<%=ModuleId %>">
            <div id="field1" class="alpaca"></div>
            <asp:Literal ID="lReCaptcha" runat="server" Mode="PassThrough"></asp:Literal>
            <ul class="dnnActions dnnClear actions-openform">
                <li>                    
                    <asp:LinkButton ID="lbSave" runat="server" class="btn btn-primary btn-openform" resourcekey="cmdSave" OnClick="lbSave_Click" />
                </li>
            </ul>
        </div>
        <span id="ResultMessage"></span>
        <div id="ResultTracking"></div>
        <asp:HiddenField ID="hfOpenForm" runat="server" ClientIDMode="Static" />        
    </asp:Panel>

    <script type="text/javascript">
        $(document).ready(function () {
            $.alpaca.setDefaultLocale("<%= AlpacaCulture %>");
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
                        $("#<%=lbSave.ClientID%>", moduleScope).click(function () {
                            selfControl.refreshValidationState(true);

                            var recaptcha = typeof (grecaptcha) != "undefined";
                            if (recaptcha) {
                                var recap = grecaptcha.getResponse();
                            }

                            if (selfControl.isValid(true) && (!recaptcha || recap.length > 0)) {
                                var value = selfControl.getValue();
                                $('#hfOpenForm').val(JSON.stringify(value));
                                if (recaptcha) {
                                    value.recaptcha = recap;
                                }
                                $(this).prop('disabled', true);                            
                                self.FormSubmit(value);
                                $(document).trigger("postSubmit.openform", [value, <%=ModuleId %>, sf]);
                            }
                            return false;
                        });
                        $(document).trigger("postRender.openform", [control, <%=ModuleId %>, sf]);
                }
            });
        }).fail(function (xhr, result, status) {
            //alert("Uh-oh, something broke: " + status);
        });

            self.FormSubmit = function (data) {
                var postData = data;
                var action = "Submit";
                $.ajax({
                    type: "POST",
                    url: sf.getServiceRoot('OpenForm') + "OpenFormAPI/" + action,
                    data: postData,
                    beforeSend: sf.setModuleHeaders
                }).done(function (data) {
                    if (data.Errors && data.Errors.length > 0) {
                        console.log(data.Errors);
                    }
                    if (data.Tracking) {
                        <%= PostBackStr() %>
                    } else {
                        $('#OpenForm', moduleScope).hide();
                        $('#ResultMessage', moduleScope).html(data.Message);
                        $('#ResultTracking', moduleScope).html(data.Tracking);
                    }
                }).fail(function (xhr, result, status) {
                    alert("Uh-oh, something broke: " + status);
                });
            };
        });
    </script>
</asp:PlaceHolder>
<asp:PlaceHolder runat="server" ID="phResult" Visible="false">
    <asp:Literal ID="lMessage" runat="server" />
    <asp:Literal ID="lTracking" runat="server" />
</asp:PlaceHolder>
