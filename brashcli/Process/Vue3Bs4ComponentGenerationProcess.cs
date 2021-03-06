using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Serilog;
using brashcli.Option;
using brashcli.Model;


namespace brashcli.Process
{
    public class VueComponentContext
    {
        public string Domain { get; private set; }
        public Structure Entity { get; private set; }
        public Structure Parent { get; private set; }
        public string IdPattern { get; private set; }
        public string EntityName { get; private set; }
        public string EntityInstanceName { get; private set; }
        
        public VueComponentContext(
            string domain
            , Structure entity
            , Structure parent
        )
        {
            Domain = domain;
            Entity = entity;
            Parent = parent;

            IdPattern = entity.IdPattern ?? Global.IDPATTERN_ASKID;
            EntityName = entity.Name;
            EntityInstanceName = ToLowerFirstChar(entity.Name);
        }

        private string ToLowerFirstChar(string input)
        {
            string newString = input;
            if (!String.IsNullOrEmpty(newString) && Char.IsUpper(newString[0]))
                newString = Char.ToLower(newString[0]) + newString.Substring(1);

            return newString;
        }

    }

    public class Vue3Bs4ComponentGenerationProcess
    {
        private ILogger _logger;
        private Vue3Bs4ComponentGeneration _options;
        private string _pathProject;
        private string _pathVueComponentDomainDirectory;
        private string _pathEntityDirectory;
        private DomainStructure _domainStructure;
        private List<string> _entities = new List<string>();

        public Vue3Bs4ComponentGenerationProcess(ILogger logger, Vue3Bs4ComponentGeneration options)
        {
            _logger = logger;
            _options = options;
            _pathProject = System.IO.Path.GetDirectoryName(_options.FilePath);
        }

        public int Execute()
        {
            int returnCode = 0;

            _logger.Debug("Vue3Bs4ComponentGenerationProcess: start");
            do
            {
                try
                {
                    ReadDataJsonFile();
                    MakeDirectories();
                    CreateComponentFiles();
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, "Vue3Bs4ComponentGenerationProcess, unhandled exception caught.");
                    returnCode = -1;
                    break;
                }

            } while (false);
            _logger.Debug("Vue3Bs4ComponentGenerationProcess: end");

            return returnCode;
        }

        private void MakeDirectories()
        {
            _pathVueComponentDomainDirectory = System.IO.Path.Combine(_options.OutputDirectory, _domainStructure.Domain);
            System.IO.Directory.CreateDirectory(_pathVueComponentDomainDirectory);
        }

        private void ReadDataJsonFile()
        {
            string json = System.IO.File.ReadAllText(_options.FilePath);
            _domainStructure = JsonConvert.DeserializeObject<DomainStructure>(json, new JsonSerializerSettings()
            {
                MissingMemberHandling = MissingMemberHandling.Ignore
            });
            _logger.Information($"Domain: {_domainStructure.Domain}, Structures: {_domainStructure.Structure.Count}");
        }

        private void CreateComponentFiles()
        {
            _logger.Debug("CreateComponentFiles");

            foreach (var entity in _domainStructure.Structure)
            {
                MakeVueComponentFiles(null, entity);
            }

        }

        private void MakeVueComponentFiles(Structure parent, Structure entity)
        {
            _logger.Debug($"{entity.Name}");

            _entities.Add(entity.Name);

            if (parent != null)
                _logger.Debug($"\t Parent: {parent.Name}");

            MakeVueComponentFileJs(parent, entity);

            if (entity.Children != null && entity.Children.Count > 0)
            {
                foreach (var child in entity.Children)
                {
                    MakeVueComponentFiles(entity, child);
                }
            }

            if (entity.Extensions != null && entity.Extensions.Count > 0)
            {
                foreach (var extension in entity.Extensions)
                {
                    MakeVueComponentFiles(entity, extension);
                }
            }
        }

        public string MakeVueComponentFilePath(Structure entity)
        {

            _pathEntityDirectory = System.IO.Path.Combine(_pathVueComponentDomainDirectory, entity.Name);
            System.IO.Directory.CreateDirectory(_pathEntityDirectory);

            return System.IO.Path.Combine(_pathEntityDirectory, entity.Name + ".vue");
        }

        private void MakeVueComponentFileJs(Structure parent, Structure entity)
        {
            string fileNamePath = MakeVueComponentFilePath(entity);
            StringBuilder lines = new StringBuilder();
            var idPattern = entity.IdPattern ?? Global.IDPATTERN_ASKID;

            lines.Append(TplVueComponent(
                _domainStructure.Domain
                , entity
                , parent
            ));

            System.IO.File.WriteAllText(fileNamePath, lines.ToString());
        }

        public string ToLowerFirstChar(string input)
        {
            string newString = input;
            if (!String.IsNullOrEmpty(newString) && Char.IsUpper(newString[0]))
                newString = Char.ToLower(newString[0]) + newString.Substring(1);

            return newString;
        }

        public string ToSpaceSeparated(string input)
        {
          StringBuilder lable = new StringBuilder();
          bool previousCharacterIsLower = false;
          bool currentCharacterIsLower = false;
          int indexToIgnore = 1;

          for( int i=0; i<input.Length; i++)
          {
            currentCharacterIsLower = Char.IsLower(input[i]);

            if (currentCharacterIsLower != previousCharacterIsLower && i != indexToIgnore)
            {
              indexToIgnore = i + 1;
              lable.Append(" ");
            }

            lable.Append(input[i]);
            previousCharacterIsLower = currentCharacterIsLower;
          }

          return lable.ToString();
        }

        private string GetFindByParentRoute(
            string domain
            , Structure entity
            , Structure parent
        )
        {
            string idPattern = entity.IdPattern ?? Global.IDPATTERN_ASKID;
            string entityName = entity.Name;
            string entityInstanceName = ToLowerFirstChar(entity.Name);
            StringBuilder lines = new StringBuilder();

            if (parent != null)
            {
                lines.Append($"\n");

                switch (parent.IdPattern)
                {
                    case Global.IDPATTERN_ASKGUID:
                        lines.Append($"\n\t\t// GET /api/{entityName}ByParent/7dd44fed-bf64-42d8-a6ea-04357c73482e");
                        lines.Append("\n\t\t[HttpGet(\"{guid}\")]");
                        lines.Append($"\n\t\tpublic ActionResult<IEnumerable<{entityName}>> GetByParent(string guid)");
                        lines.Append("\n\t\t{");
                        lines.Append($"\n\t\t\tvar queryResult = _{entityInstanceName}Service.FindByParent(guid);");
                        lines.Append("\n\t\t\tif (queryResult.Status == BrashQueryStatus.ERROR)");
                        lines.Append("\n\t\t\t\treturn BadRequest(queryResult.Message);");
                        lines.Append("\n\t\t");
                        lines.Append("\n\t\t\treturn queryResult.Models;");
                        lines.Append("\n\t\t}");
                        lines.Append("\n\t\t");

                        break;
                    case Global.IDPATTERN_ASKVERSION:
                        lines.Append($"\n\t\t// GET /api/{entityName}ByParent/7dd44fed-bf64-42d8-a6ea-04357c73482e/12");
                        lines.Append("\n\t\t[HttpGet(\"{guid}/{version}\")]");
                        lines.Append($"\n\t\tpublic ActionResult<IEnumerable<{entityName}>> GetByParent(string guid, decimal version)");
                        lines.Append("\n\t\t{");
                        lines.Append($"\n\t\t\tvar queryResult = _{entityInstanceName}Service.FindByParent(guid, version);");
                        lines.Append("\n\t\t\tif (queryResult.Status == BrashQueryStatus.ERROR)");
                        lines.Append("\n\t\t\t\treturn BadRequest(queryResult.Message);");
                        lines.Append("\n\t\t");
                        lines.Append("\n\t\t\treturn queryResult.Models;");
                        lines.Append("\n\t\t}");
                        lines.Append("\n\t\t");

                        break;
                    case Global.IDPATTERN_ASKID:
                    default:
                        lines.Append($"\n\t\t// GET /api/{entityName}ByParent/4");
                        lines.Append("\n\t\t[HttpGet(\"{id}\")]");
                        lines.Append($"\n\t\tpublic ActionResult<IEnumerable<{entityName}>> GetByParent(int id)");
                        lines.Append("\n\t\t{");
                        lines.Append($"\n\t\t\tvar queryResult = _{entityInstanceName}Service.FindByParent(id);");
                        lines.Append("\n\t\t\tif (queryResult.Status == BrashQueryStatus.ERROR)");
                        lines.Append("\n\t\t\t\treturn BadRequest(queryResult.Message);");
                        lines.Append("\n\t\t");
                        lines.Append("\n\t\t\treturn queryResult.Models;");
                        lines.Append("\n\t\t}");
                        lines.Append("\n\t\t");

                        break;
                }
            }

            return lines.ToString();
        }

        public string TplVueComponent(
            string domain
            , Structure entity
            , Structure parent
            )
        {
            VueComponentContext context = new VueComponentContext(domain, entity, parent);
            StringBuilder lines = new StringBuilder();

            lines.Append(MakeFileHeader(context));
            lines.Append(MakeTemplate(context));
            lines.Append(MakeScript(context));
            lines.Append(MakeCss(context));
            
            return lines.ToString();
        }

        public string MakeFileHeader(VueComponentContext context)
        {
            return $@"<!--
Brash Generated: { DateTime.Now }
Domain: { context.Domain }
Entity: { context.EntityName }
-->
";
        }


        public string MakeTemplate(VueComponentContext context)
        {
            StringBuilder lines = new StringBuilder();

            lines.Append(MakeTemplateStart(context));

            lines.Append(MakeTemplateAlertBox(context));
            lines.Append(MakeTemplateLoadingSpinner(context));
            lines.Append(MakeTemplateCardStart(context));
            lines.Append(MakeListing(context));
            lines.Append(MakeForm(context));
            lines.Append(MakeTemplateCardEnd(context));

            lines.Append(MakeTemplateEnd(context));

            return lines.ToString();
        }

        public string MakeScript(VueComponentContext context)
        {
            StringBuilder lines = new StringBuilder();

            lines.Append(MakeRawScript());

            return lines.ToString();
        }

        public string MakeCss(VueComponentContext context)
        {
            return $@"
<style lang=""css"">

</style>
";
        }

        
        public string MakeTemplateStart(VueComponentContext context)
        {
            return $@"<template>
  <div class=""container-fluid h-100"">

    <div class=""row justify-content-md-center"">
      <h2>{ context.EntityName }</h2>
    </div>            

";
        }

        public string MakeTemplateEnd(VueComponentContext context)
        {
            return $@"
  </div>
</template>
";
        }

        public string MakeTemplateAlertBox(VueComponentContext context)
        {
            return $@"
  <!-- alert box -->
  <div class=""alert alert-primary alert-dismissible fade show"" :class=""{{ alertCssClass }}"" v-if=""hasAlert()"">
    <button type=""button"" class=""close"" @click=""hideAlert()"">&times;</button>
    {{{{ alertMessage }}}}
  </div>

";
        }

        public string MakeTemplateLoadingSpinner(VueComponentContext context)
        {
            return $@"
  <!-- loading spinner -->
  <div class=""row justify-content-md-center"" v-if=""isLoading()"">
    <div class=""spinner-border text-primary""></div>
  </div>

";
        }

        public string MakeTemplateCardStart(VueComponentContext context)
        {
            return $@"
  <!-- title bar -->
  <div class=""row no-gutters"">
    <div class=""card col-12"">
      <div class=""card-header"">
        <ul class=""nav nav-tabs card-header-tabs"">
          <li class=""nav-item"">
            <a class=""nav-link"" href=""#"" :class=""isListingVisible() ? 'active' : ''"" @click=""showListing()"">Listing</a>
          </li>
          <li class=""nav-item"" >
            <a class=""nav-link"" href=""#"" :class=""isFormVisible() ? 'active' : ''"" @click=""showForm()"">Form</a>
          </li>
        </ul>
      </div>

      <div class=""card-body"">

";
        }

        public string MakeTemplateCardEnd(VueComponentContext context)
        {
            return $@"
        </div>
      </div>
    </div>

";
        }

        private (List<string> headers, string headerText) MakeListingHeaders(VueComponentContext context)
        {
          StringBuilder lines = new StringBuilder();
          List<string> headers = new List<string>();
          
          headers.Add("Action");

          foreach(var field in context.Entity.Fields)
          {

          }

          return (headers, lines.ToString());
        }

        public string MakeListing(VueComponentContext context)
        {
          StringBuilder headerLines = new StringBuilder();
          StringBuilder valueLines = new StringBuilder();
          List<string> headers = new List<string>();
          List<string> values = new List<string>();
          
          // add action
          headers.Add("Action");

          // add primary key
          switch(context.IdPattern)
          {
            case Global.IDPATTERN_ASKVERSION:
              headers.Add("ID");
              headers.Add("GUID");
              headers.Add("VER");
              headers.Add("Current");
              values.Add($@"item.{context.EntityInstanceName}Id");
              values.Add($@"item.{context.EntityInstanceName}Guid");
              values.Add($@"item.{context.EntityInstanceName}RecordVersion");
              values.Add($@"item.IsCurrent");
            break;
            case Global.IDPATTERN_ASKGUID:
              headers.Add("ID");
              headers.Add("GUID");
              values.Add($@"item.{context.EntityInstanceName}Id");
              values.Add($@"item.{context.EntityInstanceName}Guid");
            break;
            case Global.IDPATTERN_ASKID:
            default:
              headers.Add("ID");
              values.Add($@"item.{context.EntityInstanceName}Id");
              break;
          };

          // add fields
          if (context.Entity.Fields != null && context.Entity.Fields.Count > 0)
          {
            foreach(var field in context.Entity.Fields)
            {
              headers.Add($@"{field.Name}");
              if (field.Type == "D")
              {
                values.Add($@"translateDateTime(item.{field.Name})");
              }
              else 
              {
                values.Add($@"item.{field.Name}");
              }
            }
          }

          // add references
          if (context.Entity.References != null && context.Entity.References.Count > 0)
          {
            foreach(var reference in context.Entity.References)
            {
              headers.Add($@"{reference.ColumnName}");
              values.Add($@"translate{reference.TableName}Id(item.{reference.ColumnName})");
            }
          }

          // make header tags
          foreach(var header in headers)
          {
            headerLines.Append($@"<th scope=""col"">{{{{ {header} }}}}</th>" + "\n                  ");
          }

          // make value tags
          foreach(var value in values)
          {
            valueLines.Append($@"<td>{{{{ {value} }}}}</td>" + "\n                  ");
          }


          var text = $@"
          <!-- table listing -->
          <div class=""row justify-content-md-center"" v-if=""isListingVisible()"">
            <table class=""table"">
              <thead class=""thead-light"">
                <tr>
                  <th scope=""col"" colspan=""{headers.Count}"">
                    <button class=""btn btn-success btn-sm float-right"" title=""add new"" @click=""addForm()"">
                      <i class=""material-icons"" style=""font-size:12px;"">add</i>
                    </button>
                    <button class=""btn btn-primary btn-sm float-right"" title=""refresh listing"" @click=""onGetListing(true)"">
                      <i class=""material-icons"" style=""font-size:12px;"">refresh</i>
                    </button>
                  </th>
                </tr>
              </thead>
              <thead class=""thead-dark"">
                <tr>
                  {headerLines.ToString()}
                </tr>
              </thead>
              <tbody>
                <tr v-for='(item, index) in this.items' :key='item.todoEntryId'>
                  <td>
                    <button class=""btn btn-danger btn-sm"" title=""remove"" @click=""onRemove(index)"">
                      <i class=""material-icons"" style=""font-size:12px;"">delete</i>
                    </button>
                    <button class=""btn btn-secondary btn-sm"" title=""edit"" @click=""onEdit(index)"">
                      <i class=""material-icons"" style=""font-size:12px;"">edit</i>
                    </button>
                  </td>
                  {valueLines.ToString()}
                </tr>
              </tbody>
            </table>
          </div>        

";

          return text;
        }

        private string MakePrimaryKeyFormField(string label, string name)
        {
          return $@"
              <label for=""{name}"">{label}</label>
              <input v-model=""form.{name}"" type=""text"" id=""{name}"" class=""form-control form-control-sm col"" autocomplete=""off"" readonly>
              <br />
              
          ";
        }

        private string MakeTextFormField(string label, string name)
        {
          return $@"
              <label for=""{name}"">{ToSpaceSeparated(label)}</label>
              <input v-model=""form.{name}"" type=""text"" id=""{name}"" class=""form-control form-control-sm col"" autocomplete=""off"" required autofocus>
              <br />
              
          ";
        }

        private string MakeDateFormField(string label, string name)
        {
          return $@"
              <label for=""{name}"">{ToSpaceSeparated(label)}</label>
              <input v-model=""form.{name}"" type=""datetime-local"" id=""{name}"" class=""form-control form-control-sm col"" autocomplete=""off"">
              <br />
              
          ";
        }

        private string MakeReferenceFormField(Reference reference, string displayColumn = "choiceName")
        {
          var name = ToLowerFirstChar(reference.ColumnName);
          var listName = $"{ToLowerFirstChar(reference.TableName)}List";
          var listItemId = $"{ToLowerFirstChar(reference.TableName)}Id";
          return $@"
              <label for=""{name}"">{ToSpaceSeparated(reference.TableName)}</label>
              <select class=""form-control col"" id=""{name}"" v-model=""form.{name}"">
                <option v-for=""item in {listName}"" v-bind:key=""item.{listItemId}"" v-bind:value=""item.{listItemId}"">{{{{ item.{displayColumn} }}}}</option>
              </select>
              <br />
              
          ";
        }

        public string MakeForm(VueComponentContext context)
        {
          StringBuilder lines = new StringBuilder();

          // start form
          var start = $@"
          <!-- form -->
          <div class=""row justify-content-md-center"" v-if=""isFormVisible()"">
            <form class=""col col-lg-6 col-md-10 col-sm-12"" @submit.prevent=""onSave"">
          ";

          lines.Append(start);

          // add primary key
          switch(context.IdPattern)
          {
            case Global.IDPATTERN_ASKVERSION:
              lines.Append(MakePrimaryKeyFormField("ID", $"{context.EntityInstanceName}Id"));
              lines.Append(MakePrimaryKeyFormField("GUID", $"{context.EntityInstanceName}Guid"));
              lines.Append(MakePrimaryKeyFormField("VERSION", $"{context.EntityInstanceName}RecordVersion"));
              lines.Append(MakePrimaryKeyFormField("Current", "IsCurrent"));
            break;
            case Global.IDPATTERN_ASKGUID:
              lines.Append(MakePrimaryKeyFormField("ID", $"{context.EntityInstanceName}Id"));
              lines.Append(MakePrimaryKeyFormField("GUID", $"{context.EntityInstanceName}Guid"));
            break;
            case Global.IDPATTERN_ASKID:
            default:
              lines.Append(MakePrimaryKeyFormField("ID", $"{context.EntityInstanceName}Id"));
              break;
          };

          // add fields
          if (context.Entity.Fields != null && context.Entity.Fields.Count > 0)
          {
            foreach(var field in context.Entity.Fields)
            {
              if (field.Type == "D")
              {
                lines.Append(MakeDateFormField(field.Name, ToLowerFirstChar(field.Name)));
              }
              else 
              {
                lines.Append(MakeTextFormField(field.Name, ToLowerFirstChar(field.Name)));
              }
            }
          }

          // add references
          if (context.Entity.References != null && context.Entity.References.Count > 0)
          {
            foreach(var reference in context.Entity.References)
            {
              lines.Append(MakeReferenceFormField(reference));
            }
          }

          // end form
          var end = $@"
              <div class=""form-group row"">
                <div class=""col-12"">
                  <button type=""reset"" class=""btn btn-warning float-right"" title=""clear"" @click=""onReset"">Clear</button>
                  <button type=""submit"" class=""btn btn-primary float-right"">Save</button>
                </div>
              </div>
              <br />
              <br />

            </form>
          </div>        

";

          lines.Append(end);

          return lines.ToString();
        }

        
        private string MakeRawScript()
        {
            string originalString = $@"

<script>
/* eslint-disable */
export default {{
  name: 'TodoList'
  , mounted() {{
    this.onGetEntryStatuses();
    this.onGetListing(true);
  }}

  , data() {{

    return {{
      
      // alert data
      alert: false,
      alertMessage: """",
      alertCssClass: ""alert-primary"",

      // listing data
      listingRefreshNeeded: false,
      listingVisible: true,
      listingLoading: false,
      items: [
      ],

      // form data
      formVisible: true,
      form: {{
        todoEntryId: '',
        summary: '',
        details: null,
        dueDate: null,
        entryStatusIdRef: null
      }},

      // drop down data
      entryStatuses: [],
      
    }}
  }}

  , computed: {{
    rows() {{
      return this.items.length
    }}
  }}

  , methods: {{

    // global data helpers

    translateDateTime(dateValue) {{
      var dateFomatted = """";

      if (dateValue != null) {{
        var dateObj = new Date(dateValue);
        if (dateObj != null && dateObj instanceof Date) {{
          var hours = dateObj.getHours();
          var ampm = hours >= 12 ? 'PM' : 'AM';
          hours = hours % 12;
          hours = hours ? hours : 12; // the hour '0' should be '12'

          dateFomatted = 
                  (""00"" + (dateObj.getMonth() + 1)).slice(-2) 
                  + ""/"" + (""00"" + dateObj.getDate()).slice(-2) 
                  + ""/"" + dateObj.getFullYear() + "" "" 
                  + (""00"" + hours).slice(-2) + "":"" 
                  + (""00"" + dateObj.getMinutes()).slice(-2) 
                  + "":"" + (""00"" + dateObj.getSeconds()).slice(-2)
                  + "" ""
                  + ampm;
        }}
      }}

      return dateFomatted
    }}

    // alert methods
    , hasAlert() {{
      return this.alert;
    }}

    , hideAlert() {{
      this.alert = false;
      this.alertCssClass = ""alert-primary"";
      this.alertMessage = """";
    }}

    , setAlert(message, cssClass) {{
      this.alert = true;

      if (message == false) {{
        this.alertMessage = """";
      }} else {{
        this.alertMessage = message;
      }}

      this.alertCssClass = ""alert-primary"";
      if (cssClass != null && cssClass.length > 0) {{
        this.alertCssClass = cssClass;
      }}
    }}

    // listing methods
    , isListingVisible() {{
      return this.listingVisible;
    }}

    , isLoading() {{
      return this.listingLoading;
    }}

    , onGetListing(showStatusAlert) {{
      this.listingLoading = true;

      this.TodoListApi.get('/TodoEntry')
        .then(request => this.onGetListingSuccess(request, showStatusAlert))
        .catch(() => this.onGetListingFail())
    }}

    , onGetListingSuccess (request, showStatusAlert) {{
      this.items = request.data;

      if (this.items.length == 0) {{
        if (showStatusAlert) {{
          this.setAlert(""No data found."", ""alert-warning"");
        }}
        this.listingVisible = false;
      }}
      else {{
        if (showStatusAlert) {{
          this.setAlert(""Loaded "" + this.rows + "" records"", ""alert-primary"");
        }}
        this.listingVisible = true;
      }}

      this.listingLoading = false;
      this.listingRefreshNeeded = false;
    }}

    , onGetListingFail() {{
      this.listingLoading = false;
      this.setAlert(""An error has occurred."", ""alert-danger"");
    }}

    , showListing() {{
      this.listingVisible = true;

      if (this.listingRefreshNeeded) {{
        this.onGetListing(false);
      }}
    }}

    , toggleListingVisible() {{
      this.listingVisible = !this.listingVisible;
    }}

    // form methods
    , showForm() {{
      this.listingVisible = false;
    }}

    , addForm() {{
      this.onClearForm();
      this.listingVisible = false;
    }}

    , isFormVisible() {{
      return !this.listingVisible;
    }}

    , onClearForm() {{
      this.form.todoEntryId = '';
      this.form.summary = '';
      this.form.details = '';
      this.form.dueDate = null;
      this.form.entryStatusIdRef = null;
    }}

    , onSave(evt) {{
      evt.preventDefault();

      if (this.form.todoEntryId == '') {{
        // add
        this.TodoListApi.post('/TodoEntry', 
        {{
          ""todoEntryId"": null,
          ""summary"": this.form.summary,
          ""details"": this.form.details,
          ""dueDate"": new Date(this.form.dueDate).toJSON(),
          ""entryStatusIdRef"": this.form.entryStatusIdRef
        }})
          .then(request => this.onSaveSuccess(request))
          .catch(() => this.onSaveFail());
      }}
      else {{
        // edit
        this.TodoListApi.put('/TodoEntry/'+this.form.todoEntryId, 
        {{
          ""todoEntryId"": this.form.todoEntryId,
          ""summary"": this.form.summary,
          ""details"": this.form.details,
          ""dueDate"": new Date(this.form.dueDate).toJSON(),
          ""entryStatusIdRef"": this.form.entryStatusIdRef
        }})
          .then(request => this.onSaveSuccess(request))
          .catch(() => this.onSaveFail());
      }}
    }}

    , onSaveSuccess(request) {{
      this.setAlert(""Saved!"", ""success"");
      this.listingRefreshNeeded = true;
    }}

    , onSaveFail() {{
      this.setAlert(""An error has occurred."", ""alert-danger"");
    }}
    
    , onReset(evt) {{
      evt.preventDefault();

      // Reset our form values
      this.onClearForm();

      // Trick to reset/clear native browser form validation state
      this.formVisible = false;
      this.$nextTick(() => {{
        this.formVisible = true;
      }});

    }}

    , onEdit(idx) {{
      var item = this.items[idx];
      if (item) {{
        // Set form values
        this.form.todoEntryId = item.todoEntryId;
        this.form.summary = item.summary;
        this.form.details = item.details;
        this.form.dueDate = item.dueDate;
        this.form.entryStatusIdRef = item.entryStatusIdRef;

        this.showForm();
      }}
      else {{
        this.setAlert(""An error has occurred."", ""alert-danger"");
      }}
    }}

    , onRemove(idx) {{
      var item = this.items[idx];
      if (item) {{
        this.TodoListApi.delete('/TodoEntry/'+item.todoEntryId)
          .then(request => this.onRemoveSuccess(request))
          .catch(() => this.onRemoveFail());
      }}
      else {{
        this.setAlert(""An error has occurred."", ""alert-danger"");
      }}
    }}

    , onRemoveSuccess(req) {{
      this.onClearForm();
      this.onGetListing(false);
      this.setAlert(""Removed!"", ""success"");
    }}

    , onRemoveFail() {{
      this.setAlert(""An error has occurred."", ""alert-danger"");
    }}

    // drop downs

    // Entry Statuses
    , onGetEntryStatuses() {{
      this.TodoListApi.get('/TodoStatus')
        .then(request => this.onGetEntryStatusesSuccess(request))
        .catch(() => this.onGetEntryStatusesFail())
    }}

    , onGetEntryStatusesSuccess(request) {{
      this.entryStatuses = request.data;
    }}

    , onGetEntryStatusesFail() {{
      this.setAlert(""An error has occurred.  Unable to get entry statuses."", ""alert-danger"");
    }}

    , translateEntryStatusId(id) {{
      var descr = """";

      if (id != null) {{
        for (let item of this.entryStatuses) {{
          if (item.todoStatusId == id) {{
            descr = item.choiceName;
            break;
          }}
        }}
      }}

      return descr;
    }}

  }}
}}
</script>

<style lang=""css"">

</style>
            ";

            return originalString;
        }

    }
}