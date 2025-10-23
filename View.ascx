<%@ Control Language="C#" AutoEventWireup="false" Inherits="Satrabel.OpenForm.View" CodeBehind="View.ascx.cs" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>
<%@ Register TagPrefix="dnncl" Namespace="DotNetNuke.Web.Client.ClientResourceManagement" Assembly="DotNetNuke.Web.Client" %>
<%@ Import Namespace="Newtonsoft.Json" %>

<%-- 
<dnncl:DnnJsInclude ID="DnnJsInclude1" runat="server" FilePath="~/DesktopModules/OpenContent/js/lib/handlebars/handlebars.min.js" Priority="106" ForceProvider="DnnPageHeaderProvider" />
<dnncl:DnnJsInclude ID="DnnJsInclude2" runat="server" FilePath="~/DesktopModules/OpenContent/js/alpaca/bootstrap/alpaca.min.js" Priority="107" ForceProvider="DnnPageHeaderProvider" />
<dnncl:DnnJsInclude ID="DnnJsInclude6" runat="server" FilePath="~/DesktopModules/OpenContent/alpaca/js/fields/dnn/CheckboxField.js" Priority="110" ForceProvider="DnnPageHeaderProvider" />
--%>

<asp:Panel ID="pHelp" runat="server" Visible="false">
    <h3>Get started</h3>

    <asp:Panel ID="pTempleteExchange" runat="server" Visible="false">
        <div style="margin-bottom: 10px;">
            <asp:Label ID="scriptListLabel" runat="server" Text="Get a template : " />
            <asp:HyperLink ID="hlTempleteExchange" runat="server" Visible="false">Template Exchange</asp:HyperLink>
        </div>
    </asp:Panel>

    <div style="margin-bottom: 10px;">
        <asp:Label ID="Label3" runat="server" Text="Use a existing template : " />
        <asp:DropDownList ID="scriptList" runat="server" Visible="false" AutoPostBack="true" OnSelectedIndexChanged="scriptList_SelectedIndexChanged" />

    </div>
    <div style="margin-bottom: 10px;">
        <asp:Label ID="Label2" runat="server" Text="Or make a copy, New template name : " />
        <asp:TextBox ID="tbTemplateName" runat="server"></asp:TextBox>
        <asp:Button ID="bCopy" runat="server" OnClick="bCopyTemplate_Click" Text="Copy" />
    </div>

    <div style="margin-bottom: 10px;">
        <asp:Label ID="Label1" runat="server" Text="Define settings : " />
        <asp:HyperLink ID="hlEditSettings" runat="server" Visible="false">Template Settings</asp:HyperLink>
    </div>
    <hr />
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
        <ul class="dnnActions dnnClear openform-validation" id="fieldvalidation-<%=ModuleId %>" style="display: none; color: #b94a48;"">
            <li>
                 <asp:Label runat="server" CssClass="openform-validation-message" resourcekey="errInvalid"></asp:Label>
            </li>
        </ul>
        <span class="ResultMessage"></span>
        <div class="ResultTracking"></div>
        <asp:HiddenField ID="hfOpenForm" runat="server" />
        <input type="hidden" name="__OPENFORM<%=ModuleId %>" id="__OPENFORM<%=ModuleId %>" value="" />
    </asp:Panel>

    <script type="text/javascript">
        $(document).ready(function () {

            //var scripts = ['/DesktopModules/OpenContent/js/lib/handlebars/handlebars.min.js',
            //    '/DesktopModules/OpenContent/js/alpaca/bootstrap/alpaca.min.js',
            //    '/DesktopModules/OpenContent/alpaca/js/fields/dnn/CheckboxField.js',
            //    '/DesktopModules/OpenContent/alpaca/js/fields/dnn/DateField.js',
            //    '/DesktopModules/OpenContent/alpaca/js/fields/dnn/SummernoteField.js'];

            var scripts = <%=JsonConvert.SerializeObject(Scripts)%>;
            var getCachedScript = function (url, options) {
                options = $.extend(options || {}, {
                    dataType: "script",
                    cache: true,
                    url: url
                });
                return jQuery.ajax(options);
            };

            var res = scripts.reduce(function (prev, cur) { // chain to res later to hook on done
                return prev.then(function (data) { return getCachedScript(cur) });
            }, $.Deferred().resolve());

            //getCachedScript("/DesktopModules/OpenContent/js/lib/handlebars/handlebars.min.js")
            //    .then(function (data1) { return getCachedScript("/DesktopModules/OpenContent/js/alpaca/bootstrap/alpaca.min.js") })
            //    .then(function (data2) { return getCachedScript("/DesktopModules/OpenContent/alpaca/js/fields/dnn/CheckboxField.js") })
            //    .then(function (data3) { return getCachedScript("/DesktopModules/OpenContent/alpaca/js/fields/dnn/DateField.js") })
            //    .then(function (data4) { return getCachedScript("/DesktopModules/OpenContent/alpaca/js/fields/dnn/SummernoteField.js") })
            res.then(

                function (data5) {

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

                        if (config.view) {
                            config.view.parent = "bootstrap-create";
                        } else {
                            config.view = "bootstrap-create";
                        }

                        $(document).trigger("preRender.openform", [config, <%=ModuleId %>, sf]);
                        $(".alpaca", moduleScope).alpaca({
                            "schema": config.schema,
                            "options": config.options,
                            "data": config.data,
                            "view": config.view,
                            "connector": connector,
                            "postRender": function (control) {
                                var selfControl = control;
                                $("#<%=lbSave.ClientID%>", moduleScope).click(function () {
                                    var saveButton = this;
                                    if ($(saveButton).hasClass('disabled')) {
                                        return false;
                                    }
                                    $('#fieldvalidation-<%=ModuleId %>').hide();
                                    selfControl.refreshValidationState(true, function () {
                                        if (!selfControl.isValid(true)) {
                                            $('#fieldvalidation-<%=ModuleId %>').show();
                                            return false;
                                        }

                                        // Create the base form data
                                        var value = selfControl.getValue();
                                        $('#<%=hfOpenForm.ClientID%>').val(JSON.stringify(value));
                                        $('#__OPENFORM<%=ModuleId %>').val(JSON.stringify(value));

                                        // Disable the button & show sending state
                                        $(saveButton).addClass('disabled');
                                        $(saveButton).text("<%= GetString("Sending") %>");
                                        $(saveButton).off();

                                        // Execute reCAPTCHA v3 (if available)
                                        var siteKey = $('#recaptchaSiteKey').val();

                                        function tryExecuteRecaptcha(attempt) {
                                            attempt = attempt || 1;

                                            if (typeof grecaptcha === "undefined") {
                                                console.warn("reCAPTCHA not yet defined; retrying in 200 ms...");
                                                return setTimeout(function () { tryExecuteRecaptcha(attempt + 1); }, 200);
                                            }

                                            grecaptcha.ready(function () {
                                                try {
                                                    grecaptcha.execute(siteKey, { action: 'form_submit' }).then(function (token) {
                                                        value.recaptcha = token;
                                                        submitForm(value);
                                                    }).catch(function (err) {
                                                        console.error("execute() failed:", err);
                                                        if (attempt < 5) {
                                                            setTimeout(function () { tryExecuteRecaptcha(attempt + 1); }, 250);
                                                        } else {
                                                            alert("reCAPTCHA failed to initialise. Please reload and try again.");
                                                        }
                                                    });
                                                } catch (ex) {
                                                    console.warn("No reCAPTCHA client yet; retrying…");
                                                    if (attempt < 5) {
                                                        setTimeout(function () { tryExecuteRecaptcha(attempt + 1); }, 250);
                                                    } else {
                                                        alert("reCAPTCHA still not ready after several attempts.");
                                                    }
                                                }
                                            });
                                        }

                                        // helper to post form
                                        function submitForm(value) {
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

                                        // kick off token request
                                        tryExecuteRecaptcha();

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
                                $('.OpenForm', moduleScope).hide();
                                $('.ResultMessage', moduleScope).html("<b>Error on submit</b><br />" + data.Errors.join("<br />"));
                                $('.ResultTracking', moduleScope).html(data.Tracking);
                                $(document.body).scrollTop(Math.max($('.OpenForm', moduleScope).offset().top - 100, 0));
                            }
                            else {
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
                            }
                        }).fail(function (xhr, result, status) {
                            alert("Uh-oh, something broke: " + status);
                        });
                    };
                }, function (x, y, z) {
                    console.log(x);
                    console.log(y);
                    console.log(z);
                });
        });
    </script>
</asp:PlaceHolder>
<asp:PlaceHolder runat="server" ID="phResult" Visible="false">
    <asp:Literal ID="lMessage" runat="server" />
    <asp:Literal ID="lTracking" runat="server" />
</asp:PlaceHolder>
