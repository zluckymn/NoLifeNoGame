
<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="Yinhe.ProcessingCenter.ViewPageBase" %>

<%@ Import Namespace="Yinhe.ProcessingCenter.Document" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.DataRule"%>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="HeadContent" runat="server">
 <script src="<%=SysAppConfig.HostDomain %>/Scripts/Reference/jquery-1.7.2.min.js"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/Common/YH.js" type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/Common/jquery.bgiframe.js"
    type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/Common/popbox.js" type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/Common/CommonFunc.js"
    type="text/javascript"></script>
    <script src="<%=SysAppConfig.HostDomain %>/Scripts/Reference/jQuery/Alerts/jquery.hiAlerts-min.js"
    type="text/javascript"></script>
<link href="<%=SysAppConfig.HostDomain %>/Scripts/Reference/jQuery/Alerts/jquery.hiAlerts.css"
    rel="stylesheet" type="text/css" />
    <style type="text/css">
        .input_select
        {
            width: 100%;
        }
    </style>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="MainContent" runat="server">
<%
    var tableName = PageReq.GetParam("tableName");
    var showOrder = PageReq.GetParamInt("showOrder");
    var showPrimKey = PageReq.GetParamInt("showPrimKey");
    var condition = PageReq.GetParam("condition").Replace("|","&").Replace("@","=");
    var columnCount = PageReq.GetParamInt("columnCount");
    var isFile = PageReq.GetParamInt("isFile");//是否文件类型
    
     var tableTitle = string.Empty;
     TableRule tableEntity=null;
     List<BsonDocument> methodList=new List<BsonDocument>();
     List<ColumnRule>columnList=new List<ColumnRule>();
     string primaryKey = "";
     if(!string.IsNullOrEmpty(tableName))
     {
         if (!string.IsNullOrEmpty(condition))
         {
             methodList = dataOp.FindAllByQueryStr(tableName,condition).OrderBy(t => t.Int("order")).ToList();
             //condition A=1&B=2
             if (!string.IsNullOrEmpty(condition))
             { 
                
             }
         }
         else
         {
             methodList = dataOp.FindAll(tableName).OrderBy(t => t.Int("order")).ToList();
         }
     tableEntity = new TableRule(tableName);    //获取表结构
     if (columnCount > 0)
     {
         columnList = tableEntity.ColumnRules.Take(columnCount).ToList();
     }
     else
     {
         columnList = tableEntity.ColumnRules.ToList();
     }
     
    if (columnList.Count > 0)
    {
        var keyFieldObj= columnList.Where(t => t.IsPrimary == true).FirstOrDefault();
        if (keyFieldObj!=null)
        primaryKey = keyFieldObj.Name;  //寻找默认主键
    }
    tableTitle = tableEntity.Remark;
    }

     var fileRelationList = new List<BsonDocument>();
     if (isFile == 1)//展示文件对应挂接名称
     {
         fileRelationList = dataOp.FindAllByQuery("FileRelation", Query.In("fileId", methodList.Select(c => (BsonValue)c.Text("fileId")))).ToList();
        
     }
    
    var tableDic=new Dictionary<string,List<BsonDocument>>();
    var allUserList = dataOp.FindAll("SysUser").ToList();
%>
<div class="p-content p10">
<div class="p-pageInfoBlock">
    <ul class="yh-breadcrumb">
        <li>管理<%=tableTitle%><i>>></i></li><li>查询个数（<%=methodList.Count()%>）</li></ul>
</div>
<div>
<%if (columnList.Count > 0 && !string.IsNullOrEmpty(primaryKey))
  { %>
   <div class="mt10 mb5">
       <a class="p-btn_1" href="javascript:" onclick="methodEdit('');">新增</a> 
    </div>
    <table width="100%" class="p-tableborder setting_item">
        <tr class="p-tableborder-title">
            <td width="60" align="center">
                自动编号
            </td>
            <%foreach (var column in columnList )
              {
                  if (showPrimKey!=1 && column.Name == primaryKey)
                  {
                      continue;
                  }
                 %>
              <td width="100">
                <%=  column.Remark%>
              </td>
            <%} %>
            <%if (showOrder == 1)
              { %>
                <td width="80">
                 顺序
              </td>
              <%} %>
            <td align="center" width="70">
                操作
            </td>
        </tr>
        <%  var index = 0;
            foreach (var method in methodList)
            {
              
        %>
        <tr primaryKey=<%=method.Text(primaryKey) %> >
            <td align="center">
                <%=++index%>
            </td>
            <%foreach (var column in columnList)
              {
                  if (showPrimKey != 1 && column.Name == primaryKey)
                  {
                      continue;
                  }
                  var content = string.Empty;
                  switch (column.Name)
                  {
                      case "createUserId":
                          var createUser = allUserList.Where(c => c.Int("userId") == method.Int("createUserId")).FirstOrDefault();
                          if (createUser != null)
                          {
                              content = createUser.Text("name");
                          }
                          else
                          {
                              content = method.CreateUserName();
                          }
                          
                          break;
                      case "updateUserId":
                          var updateUser = allUserList.Where(c => c.Int("userId") == method.Int("updateUserId")).FirstOrDefault();
                          if (updateUser != null)
                          {
                              content = updateUser.Text("name");
                          }
                          else
                          {
                              content = method.UpdateUserName();
                          }
                          break;
                      case "createDate": content = method.CreateDate(); break;
                      case "updateDate": content = method.CreateDate(); break;
                      case "name":
                          if (isFile == 1)
                          {
                              var fileSourceObj = fileRelationList.Where(c => c.Text("fileId") == method.Text("fileId")).FirstOrDefault();
                              if (fileSourceObj != null)
                              {
                                  var fileTableName = fileSourceObj.Text("tableName");
                                  var fileKeyName = fileSourceObj.Text("keyName");
                                  var fileKeyValue = fileSourceObj.Text("keyValue");
                                  if (!tableDic.ContainsKey(fileTableName))
                                  {
                                      var fileSourceList = dataOp.FindAll(fileTableName).ToList();
                                      tableDic.Add(fileTableName, fileSourceList);
                                  }
                                  var hitSourceObj = tableDic[fileTableName].Where(c => c.Text(fileKeyName) == fileKeyValue).FirstOrDefault();
                                  if (hitSourceObj != null)
                                  {
                                      var linkUrl = string.Empty;
                                      switch (fileTableName)
                                      {
                                          case "Material_Material":
                                          case "XH_Material_Material":
                                              linkUrl = string.Format("/Material/MaterialShow?matId={0}&isEdit=0", fileKeyValue);
                                              content = string.Format(@"<a href='{0}' target='_blank'>{1}</a>-{2}",linkUrl, hitSourceObj.Text("name"), method.Text("name"));
                                             
                                              break;
                                         case "XH_DesignManage_Task":
                                              linkUrl = string.Format("/DesignManage/ProjTaskInfo/{0}", fileKeyValue);
                                              content = string.Format(@"<a href='{0}'  target='_blank'>{1}</a>-{2}", linkUrl, hitSourceObj.Text("name"), method.Text("name"));
                                              break;
                                          default: content = string.Format("{0}-{1}", hitSourceObj.Text("name"), method.Text("name")); break;
                                      }
                                     
                                  }
                                  else
                                  {
                                      content = method.Text("name");
                                  }
                                  
                              }
                          }
                          else
                          { 
                          content = method.Text("name");
                          }

                          break;
                      default:
                          if (!string.IsNullOrEmpty(column.SourceTable))
                          {
                              var sourceKeyFiled = column.SourceColumn.Trim();
                              var sourceObj = new BsonDocument();
                              if (tableDic.ContainsKey(column.SourceTable))
                              {
                                  sourceObj = tableDic[column.SourceTable].Where(c => c.Text(sourceKeyFiled) == method.String(column.Name)).FirstOrDefault();
                                  
                              }
                              else
                              {
                                  var dataList = dataOp.FindAll(column.SourceTable).ToList();
                                    tableDic.Add(column.SourceTable, dataList);
                                     sourceObj = dataList.Where(c => c.Text(sourceKeyFiled) == method.String(column.Name)).FirstOrDefault();
                                  
                                  //else
                                  //{
                                  //    sourceObj = dataOp.FindOneByQuery(column.SourceTable, Query.EQ(sourceKeyFiled, method.String(column.Name)));
                                  //}
                              }
                         
                              if (sourceObj != null)
                              {
                                  content = sourceObj.String("name");
                              }

                          }
                          else
                          {
                              content = method.String(column.Name);
                          }
                          break;
                  }
                  
                  
                  %>
            <td>
                <%=content%>
            </td>
           
            <%} %>
             <%if (showOrder == 1)
              { %>
                <td>
                <%=method.String("order")%>
            </td>
            <%} %>
            <td align="center">
              
                <a href="javascript:;" onclick='methodEdit("<%=method.String(primaryKey) %>");' class="green">
                    编辑</a>&nbsp;&nbsp; 
                     <%if (index > 1)
                       { %>
                    <a href="javascript:void(0);" onclick="moveOrder(this,'pre')">上移</a>
                    <%} %>
                    <%if (index < methodList.Count())
                      { %>
                    <a href="javascript:void(0);" onclick="moveOrder(this,'next')">下移</a>
                    <%} %>
                    <a class="red" href="javascript:void(0);" tbname="<%=tableName %>"
                        querystr="db.<%=tableName %>.distinct('_id',{'<%=primaryKey %>':'<%=method.String(primaryKey) %>'})"
                        onclick="CustomDeleteItem(this,reload);">删除</a>
                      
            </td>
        </tr>
        <% } %>
    </table>
   
    <%}
  else
  { %>
  传入表名不存在或者对应插件不存在对应表重新输入对应表名，区分大小写,可选参数  showOrder  showPrimKey
  <div style="padding-top: 5px;">
  <input type="text" value="<%=tableName %>" id="tableName"/>
       <a class="N-btn0004" href="javascript:" onclick="jumpUrl();">
            <img src="/Content/images/icon/icon003a2.gif" />确定</a> 
    </div>
    <%} %>
</div>
</div>
<script type="text/javascript">
    SetMenu("系统设置", 3);

    function moveOrder(obj, type) {
        var moveId = $(obj).parents("tr").attr("primaryKey");
        var moveToId = "";
        var newType = "";
        if (type == "pre") {
            moveToId = $(obj).parents("tr").prev().attr("primaryKey");
            newType = "pre";
        }
        else {
            moveToId = $(obj).parents("tr").next().attr("primaryKey");
            newType = "next";
        }
        $.ajax({
            url: "/Home/MovePostInfo",
            type: 'post',
            data: {
                tbName: "<%=tableName %>",
                type: newType,
                moveId: moveId,
                moveToId: moveToId
            },
            dataType: 'json',
            error: function () {
                $.tmsg("m_jfw", "未知错误，请联系服务器管理员，或者刷新页面重试", { infotype: 2 });
            },
            success: function (data) {
                if (data.Success == false) {
                    alert("保存失败")
                }
                else {
                   window.location.reload();
                }
            }
        });
    }


    function jumpUrl() {
        var tableName = $("#tableName").val();
        if (tableName !="") {
            window.location.href = "/Home/CommonTableManage/?tableName=" + tableName + "&random=" + Math.random();

        } else
        { $.tmsg("m_jfw", "表名不能为空！", { infotype: 2 }); }
    
    }


    function methodEdit(id) {
        var title = "编辑<%=tableTitle %>"
        if (id == 0)
            title = "新增<%=tableTitle %>";
        box('/Home/CommonTableEdit?&showOrder=<%=showOrder %>&showPrimKey=<%=showPrimKey %>&tableName=<%=tableName %>&<%=primaryKey %>=' + id, { boxid: 'methodEdit', title: title, width: 350, contentType: 'ajax',
            submit_cb: function (o) {
                var obj = o.fbox.find("form");
                saveInfo(obj);
            }
        });
    }

    function saveInfo(obj) {
        var formdata = $(obj).serialize();
        $.ajax({
            url: "/Home/SavePostInfo",
            type: 'post',
            data: formdata,
            dataType: 'json',
            error: function () {
                $.tmsg("m_jfw", "未知错误，请联系服务器管理员，或者刷新页面重试", { infotype: 2 });
            },
            success: function (data) {
                if (data.Success == false) {
                    $.tmsg("m_jfw", data.Message, { infotype: 2 });
                }
                else {
                    $.tmsg("m_jfw", "保存成功！", { infotype: 1 });
                    reload();
                }
            }
        });
    }

    function reload() {
        window.location.href = window.location.href;
    }

</script>
</asp:Content>
