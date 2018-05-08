<%@ Control Language="C#" AutoEventWireup="false" Inherits="Satrabel.OpenForm.View" CodeBehind="View.ascx.cs" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>
<%@ Register TagPrefix="dnncl" Namespace="DotNetNuke.Web.Client.ClientResourceManagement" Assembly="DotNetNuke.Web.Client" %>
<dnncl:DnnJsInclude ID="DnnJsInclude1" runat="server" FilePath="~/DesktopModules/OpenContent/js/lib/handlebars/handlebars.js" Priority="106" ForceProvider="DnnPageHeaderProvider" />
<dnncl:DnnJsInclude ID="DnnJsInclude2" runat="server" FilePath="~/DesktopModules/OpenContent/js/alpaca/bootstrap/alpaca.js" Priority="107" ForceProvider="DnnPageHeaderProvider" />

<dnncl:DnnJsInclude ID="DnnJsInclude4" runat="server" FilePath="~/DesktopModules/OpenContent/js/lib/moment/min/moment-with-locales.min.js" Priority="108" ForceProvider="DnnPageHeaderProvider" />
<dnncl:DnnJsInclude ID="DnnJsInclude3" runat="server" FilePath="~/DesktopModules/OpenContent/js/lib/eonasdan-bootstrap-datetimepicker/build/js/bootstrap-datetimepicker.min.js" Priority="109" ForceProvider="DnnPageHeaderProvider" />
<dnncl:DnnCssInclude ID="DnnCssInclude4" runat="server" FilePath="~/DesktopModules/OpenContent/js/lib/eonasdan-bootstrap-datetimepicker/build/css/bootstrap-datetimepicker.min.css" />
<dnncl:DnnJsInclude ID="DnnJsInclude5" runat="server" FilePath="~/DesktopModules/OpenContent/alpaca/js/fields/dnn/DateField.js" Priority="110" ForceProvider="DnnPageHeaderProvider" />
           
<dnncl:DnnCssInclude ID="DnnCssInclude1" runat="server" FilePath="~/DesktopModules/OpenContent/js/summernote/summernote.css" />
<dnncl:DnnJsInclude ID="DnnJsInclude10" runat="server" FilePath="~/DesktopModules/OpenContent/js/summernote/summernote.js" Priority="113" ForceProvider="DnnPageHeaderProvider" />
<dnncl:DnnJsInclude ID="DnnJsInclude9" runat="server" FilePath="~/DesktopModules/OpenContent/alpaca/js/fields/dnn/SummernoteField.js" Priority="113" ForceProvider="DnnPageHeaderProvider" />

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
        <div id="OpenForm<%=ModuleId %>" class="OpenForm OpenForm<%=ModuleId %>">
            <div id="field-<%=ModuleId %>" class="alpaca"></div>
            <asp:Literal ID="lReCaptcha" runat="server" Mode="PassThrough"></asp:Literal>
            <ul class="dnnActions dnnClear actions-openform">
                <li>
                    <asp:LinkButton ID="lbSave" runat="server" class="btn btn-primary btn-openform" resourcekey="cmdSave" OnClick="lbSave_Click" />
                </li>
            </ul>
        </div>
        <span class="ResultMessage"></span>
        <div class="ResultTracking"></div>
        <asp:HiddenField ID="hfOpenForm" runat="server" />
        <asp:TextBox ID="tbOpenForm" runat="server" CssClass="hidden"></asp:TextBox>
        <input type="hidden" name="__OPENFORM" id="__OPENFORM" value="" />
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

                $(".alpaca", moduleScope).alpaca({
                    "schema": config.schema,
                    "options": config.options,
                    "data": config.data,
                    "view": view,
                    "connector": connector,
                    "postRender": function (control) {
                        var selfControl = control;
                        $("#<%=lbSave.ClientID%>", moduleScope).click(function () {
                            var saveButton = this;
                            if ($(saveButton).hasClass('disabled')) {
                                return false;
                            }
                            selfControl.refreshValidationState(true, function () {
                                var recaptcha = typeof (grecaptcha) != "undefined";
                                if (recaptcha) {
                                    var recap = grecaptcha.getResponse();
                                }
                                if (selfControl.isValid(true) && (!recaptcha || recap.length > 0)) {
                                    var value = selfControl.getValue();
                                    $('#<%=hfOpenForm.ClientID %>').val(JSON.stringify(value));
                                    $('#__OPENFORM').val(JSON.stringify(value));
                                    if (recaptcha) {
                                        value.recaptcha = recap;
                                    }
                                    $(saveButton).addClass('disabled');
                                    $(saveButton).text("<%= GetString("Sending") %>");
                                    $(saveButton).off();
                                    var fd = new FormData();
                                    $(".alpaca .alpaca-field-file input[type='file']").each(function () {
                                        var file_data = $(this).prop("files")[0];
                                        var name = $(this).attr("name");
                                        fd.append(name, file_data);

                                    });
                                    fd.append("data", JSON.stringify(value));
                                    self.FormSubmit(fd, value);
                                    $(document).trigger("postSubmit.openform", [value, <%=ModuleId %>, sf]);
                                }
                            });
                            return false;
                        });
                        $(document).trigger("postRender.openform", [control, <%=ModuleId %>, sf]);
                    }
                });
            }).fail(function (xhr, result, status) {
                //alert("Uh-oh, something broke: " + status);
            });

            self.FormSubmit = function (formdata, value) {
                $.ajax({
                    type: "POST",
                    url: sf.getServiceRoot('OpenForm') + "OpenFormAPI/Submit",
                    contentType: false,
                    processData: false,
                    data: formdata,
                    beforeSend: sf.setModuleHeaders
                }).done(function (data) {
                    if (data.Errors && data.Errors.length > 0) {
                        console.log(data.Errors);
                    }
                    if (data.Tracking || data.AfterSubmit) {
                        //var jsonData = JSON.stringify(value);
                        <%= PostBackStr() %>
                        //WebForm_DoPostBackWithOptions(new WebForm_PostBackOptions("dnn$ctr472$View$lbSave", jsonData, false, "", "http://localhost:54068/fr-fr/openform/result/submit", false, true))

                        //window.location = window.location + "/submit/" + encodeURIComponent(JSON.stringify(value));
                    } else {
                        $('.OpenForm', moduleScope).hide();
                        $('.ResultMessage', moduleScope).html(data.Message);
                        $('.ResultTracking', moduleScope).html(data.Tracking);
                        $(document.body).scrollTop(Math.max($('.OpenForm', moduleScope).offset().top - 100, 0));
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
