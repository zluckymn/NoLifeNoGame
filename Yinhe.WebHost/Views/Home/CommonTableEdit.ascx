<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.DataRule"%>
<%

    var tableName = PageReq.GetParam("tableName");
    var fileObjId = PageReq.GetParam("fileObjId");//是否有文件上传
    var isBoolField = PageReq.GetParamList("isBoolField").ToList();  // 为布尔选项
      
    var tableTitle = string.Empty;

    var methodList = dataOp.FindAll("tableName").OrderBy(t => t.Int("order")).ToList();
    var tableEntity = new TableRule(tableName);    //获取表结构

    string primaryKey = "";

    var columnList = tableEntity.ColumnRules.ToList();
    if (columnList.Count > 0)
    {
        primaryKey = columnList.Where(t => t.IsPrimary == true).FirstOrDefault().Name;  //寻找默认主键
    }
    tableTitle = tableEntity.Remark;

  

    int  primaryKeyValue = PageReq.GetParamInt(primaryKey);

    BsonDocument entity = new BsonDocument();
    string updateQuery = "";                    //更新查询语句

    if (primaryKeyValue!=0)
    {
        entity = dataOp.FindOneByKeyVal(tableName, primaryKey, primaryKeyValue.ToString());
        updateQuery = "db." + tableName + ".distinct('_id',{'" + primaryKey + "':'" + primaryKeyValue.ToString() + "'})";
    }
    var showOrder = PageReq.GetParamInt("showOrder");
    var showPrimKey = PageReq.GetParamInt("showPrimKey");
    var filterColumnList=new List<string>();
    if (showPrimKey != 1)
    {
        filterColumnList.Add(primaryKey);
    }
  //  filterColumnList.Add("order");
    filterColumnList.Add("updateUserId");
    filterColumnList.Add("createUserId");
  
    var areaList=dataOp.FindAll("LandArea").ToList();
%>
<form action="/Home/SavePostInfo" method="post">
<%if (!string.IsNullOrEmpty(fileObjId))
  { %>
<input type="hidden" id="fileTypeId" name="fileTypeId" value="0" />
<input type="hidden" id="fileObjId" name="fileObjId" value="0" />
<input type="hidden" id="keyValue" name="keyValue" value="0" />
<input type="hidden" id="tableName" name="tableName" value="0" />
<input type="hidden" id="keyName" name="keyName" value="0" />
<%} %>
<input type="hidden" name="tbName" value="<%=tableName %>" />
<input type="hidden" name="queryStr" value="<%=updateQuery %>" />
<div class="boxmargin">
    <table width="100%" class="m10">
    <%foreach (var column in columnList) {
          if (filterColumnList.Contains(column.Name))
          { continue; }
          var defaultSelect = entity.String(column.Name);
          if (defaultSelect == "" && column.Name == "userId")
          {
              defaultSelect = this.CurrentUserId.ToString(); 
          }
          var columnName = column.Name;
          if (!string.IsNullOrEmpty(fileObjId))
          {
              if (column.Name == "tableName" || column.Name == "keyValue" || column.Name == "keyName" || column.Name == "fileTypeId" || column.Name == "fileObjId")
              {
                  column.Name = string.Format("COLUMNNEEDCONVERT_{0}", column.Name);
              }
          }
          %>
        <tr>
            <td height="30" width="130">
                <%=column.Remark%>：
            </td>
            <td>
            <%if (!string.IsNullOrEmpty(column.SourceTable))
              {
                  var sourceTableList = dataOp.FindAll(column.SourceTable).OrderBy(c=>c.Int("order")).ToList();
                  var sourceKeyFiled = column.SourceColumn.Trim();
                  
                %>
               <select sourceKeyFiled="<%=sourceKeyFiled %>" style="width:170px" name="<%=columnName %>">
               <option value="">请选择</option>
               <%foreach (var sourceObj in sourceTableList) {%>
               <option  <%if(sourceObj.Text(sourceKeyFiled)==defaultSelect){ %>selected<%} %> value="<%=sourceObj.Text(sourceKeyFiled) %>"><%=sourceObj.Text("name")%></option>
               <%} %>
               </select>
            <%}
              else if (isBoolField.Count > 0 && isBoolField.Contains(columnName))
              {
                  %>                 
                  <input type="radio" value="1" name="<%=column.Name %>" <%if(entity.Int(columnName)==1) {%> checked="checked" <%} %> />是
                  <input type="radio" value="0" name="<%=column.Name %>" <%if(entity.Int(columnName)==0) {%> checked="checked" <%} %> />否
              <%}
              else
              {
                  %>
               <input type="text" style="width:170px" name="<%=column.Name %>" value="<%=entity.String(columnName)%>" />
            <%} %>
            </td>
        </tr>
        <%} %>
        <%if(showOrder==1) {%>
           <tr>
            <td height="30" width="90">
                顺序：
            </td>
            <td>
                <input type="text" style="width:170px" name="order" value="<%=entity.String("order")%>" />
            </td>
        </tr>
        <%} %>

         <%if (!string.IsNullOrEmpty(fileObjId) && fileObjId!="0")
           {%>
           <tr>
            <td height="30" width="90">
                上传：
            </td>
            <td>
                 <div id="fileDiv">   </div>
            </td>
        </tr>
        <%} %>
        <%if (tableName == "FlowArea")
          {%>
        <tr>
            <td height="30" width="90">
                关联区域：
            </td>
            <td>
                 <select name="sysAreaId" >
                 <option value="0" <%if(entity.String("sysAreaId")=="0"||entity.String("sysAreaId")==""){ %> selected="selected"<%} %> >请选择区域</option>
                 <%foreach (var tempArea in areaList.OrderBy(c => c.Int("order")))
                   { %>
                   <option value="<%=tempArea.String("areaId") %>"  <%if(tempArea.String("areaId")==entity.String("sysAreaId")){ %> selected="selected"<%} %> ><%=tempArea.String("name")%></option>
                 <%} %>
                 </select>
            </td>
        </tr>
        <%} %>
    </table>
</div>
</form>
<script>
    $(function () {
        
        var lurl = "/Home/CommonTablePhotoEdit?tableName=<%=tableName%>&primaryKey=<%=primaryKey%>&primaryKeyValue=<%=primaryKeyValue %>&fileObjId=<%=fileObjId %>&r=" + Math.random();
        $("#fileDiv").load(lurl + "&r=" + Math.random());
        //window.open(lurl);
        $("#fileDiv").attr("lurl", lurl);
    });


    function resetDiv() {
        $("#fileDiv").load($("#fileDiv").attr("lurl")+"&r=" + Math.random());
    }
    function resetDivTime() {
        setTimeout("resetDiv()", 2000);
    }

</script>
