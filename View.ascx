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
    <div id="OpenForm" class="OpenForm<%=ModuleId %>">
        <div id="field1" class="alpaca"></div>
        <ul class="dnnActions dnnClear" style="display: block; padding-left: 35%">
            <li><asp:HyperLink ID="cmdSave" runat="server" class="btn btn-primary" resourcekey="cmdSave" /></li>
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
            $.alpaca.Fields.DnnFileField = $.alpaca.Fields.FileField.extend({
                setup: function () {
                    this.base();
                },
                afterRenderControl: function (model, callback) {
                    var self = this;
                    this.base(model, function () {
                        self.handlePostRender(function () {
                            callback();
                        });
                    });
                },
                handlePostRender: function (callback) {
                    var el = this.control;
                    self.SetupFileUpload(el);
                    callback();
                }
            });
            Alpaca.registerFieldClass("file", Alpaca.Fields.DnnFileField);

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
                        }
                        return false;
                    });
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
        self.SetupFileUpload = function (fileupload) {
            $(fileupload).fileupload({
                dataType: 'json',
                url: sf.getServiceRoot('OpenForm') + "FileUpload/UploadFile",
                maxFileSize: 25000000,
                formData: { example: 'test' },
                beforeSend: sf.setModuleHeaders,
                add: function (e, data) {
                    //data.context = $(opts.progressContextSelector);
                    //data.context.find($(opts.progressFileNameSelector)).html(data.files[0].name);
                    //data.context.show('fade');
                    data.submit();
                },
                progress: function (e, data) {
                    if (data.context) {
                        var progress = parseInt(data.loaded / data.total * 100, 10);
                        data.context.find(opts.progressBarSelector).css('width', progress + '%').find('span').html(progress + '%');
                    }
                },
                done: function (e, data) {
                    if (data.result) {
                        $.each(data.result, function (index, file) {
                            //$('<p/>').text(file.name).appendTo($(e.target).parent().parent());
                            //$('<img/>').attr('src', file.url).appendTo($(e.target).parent().parent());

                            $(e.target).closest('.alpaca-container').find('.alpaca-field-image input').val(file.url);
                            $(e.target).closest('.alpaca-container').find('.alpaca-image-display img').attr('src', file.url);
                        });
                    }
                }
            }).data('loaded', true);
        }
    });
</script>
