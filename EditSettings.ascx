<%@ Control Language="C#" AutoEventWireup="false" Inherits="Satrabel.OpenForm.EditSettings" CodeBehind="EditSettings.ascx.cs" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>
<%@ Register TagPrefix="dnncl" Namespace="DotNetNuke.Web.Client.ClientResourceManagement" Assembly="DotNetNuke.Web.Client" %>


<asp:Panel ID="ScopeWrapper" runat="server">
    <div class="dnnFormItem">
        <dnn:Label ID="scriptListLabel" ControlName="scriptList" runat="server" />
        <asp:DropDownList ID="scriptList" runat="server" />
        <asp:HyperLink visible="false" ID="hlTemplateExchange" runat="server">More...</asp:HyperLink>
    </div>
    <div id="field1" class="alpaca"></div>
    <asp:CustomValidator ID="CustomValidator" runat="server" ErrorMessage="CustomValidator" ControlToValidate="scriptList" ClientValidationFunction="ClientValidation"></asp:CustomValidator>
    <asp:HiddenField ID="HiddenField" runat="server" />
    <ul class="dnnActions dnnClear" style="display: block; padding-left: 35%">
        <li>
            <asp:LinkButton ID="cmdSave" runat="server" class="dnnPrimaryAction" resourcekey="cmdSave" OnClick="cmdSave_Click" /></li>
        <li>
            <asp:HyperLink ID="hlCancel" runat="server" class="dnnSecondaryAction" resourcekey="cmdCancel" />
        </li>
    </ul>
</asp:Panel>

<script type="text/javascript">
    function ClientValidation(sender, args) {
        var alp = $("#field1").alpaca("get");
        if (alp) {
            alp.refreshValidationState(true);
            if (alp.isValid(true)) {
                var value = alp.getValue();
                $("#<%= HiddenField.ClientID %>").val(JSON.stringify(value, null, "  "));
                return;
            }
            args.IsValid = false;
        }
        return;
    }

    /*globals jQuery, window, Sys */
    (function ($, Sys) {
        function setupStructSettings() {
            var moduleScope = $('#<%=ScopeWrapper.ClientID %>'),
            self = moduleScope,
            sf = $.ServicesFramework(<%=ModuleId %>);

            $("#<%= scriptList.ClientID %>").change(function () {
                $("#field1").alpaca("destroy");
                self.CreateForm();
            });

            self.CreateForm = function () {
                var Template = $("#<%= scriptList.ClientID %>").val();
                if (!Template) return;
                var postData = {};
                var getData = "Template=" + Template;
                var action = "Settings";
                $.ajax({
                    type: "GET",
                    url: sf.getServiceRoot('OpenForm') + "OpenFormAPI/" + action,
                    data: getData,
                    beforeSend: sf.setModuleHeaders
                }).done(function (config) {
                    if (config.schema) {
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
                                //var self = this;
                                var el = this.control;
                                self.SetupFileUpload(el);
                                callback();
                            }
                        });
                        Alpaca.registerFieldClass("file", Alpaca.Fields.DnnFileField);

                        $("#field1").alpaca({
                            "schema": config.schema,
                            "options": config.options,
                            "data": config.data,
                            "view": "dnnbootstrap-edit",
                            "connector": connector,
                            "postRender": function (control) {
                            }
                        });
                    }
                }).fail(function (xhr, result, status) {
                    alert(status + " : " + xhr.responseText);
                });
            };
            self.FormSubmit = function (data, href) {
                var postData = { form: data };
                var action = "Update"; 
                $.ajax({
                    type: "POST",
                    url: sf.getServiceRoot('OpenForm') + "OpenFormAPI/" + action,
                    data: postData,
                    beforeSend: sf.setModuleHeaders
                }).done(function (data) {
                    window.location.href = href;
                }).fail(function (xhr, result, status) {
                    alert("Uh-oh, something broke: " + status + " " + xhr.responseText);
                });
            };
            self.SetupFileUpload = function (fileupload) {
                $(fileupload).fileupload({
                    dataType: 'json',
                    url: sf.getServiceRoot('Satrabel.Content') + "FileUpload/UploadFile",
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
            self.CreateForm();
        }
        $(document).ready(function () {
            setupStructSettings();
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                setupStructSettings();
            });
        });
    }(jQuery, window.Sys));
</script>
