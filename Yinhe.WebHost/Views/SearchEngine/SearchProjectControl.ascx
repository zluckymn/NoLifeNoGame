<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Permissions" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Common" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Document" %>
<%
   
    var provinceId = PageReq.GetParamInt("provinceId");
    var cityId = PageReq.GetParamInt("cityId");
    var totalPage =0;
    var keyWord =Server.UrlDecode(PageReq.GetParam("strKeyWord"));
    var search = new Yinhe.ProcessingCenter.MvcFilters.CommonSearch("XH_DesignManage_Project");
    var current=PageReq.GetParamInt("current");
    if (current <= 0)
    {
        current = 1;
    }
    var keyFieldList = new List<string>();
    keyFieldList.Add("name");
    keyFieldList.Add("designManager");
    var projectList = new List<BsonDocument>();
    projectList = search.QuickSearch(current, out totalPage, keyFieldList, keyWord);
     
    //区域过滤
    if(provinceId!=0)
    {
     projectList=projectList.Where(c=>c.Int("areaId")==provinceId).ToList();
    }
    //城市过滤
    if (cityId != 0)
    {
        projectList = projectList.Where(c => c.SourceBsonField("engId","cityId") == cityId.ToString()).ToList();
    }
    
    var startIndex = (current - 1) * SysAppConfig.PageSize + 1;
    var endIndex = current + projectList.Count() - 1;
    
%>
<div class="structlisttop" style="margin-top: 30px;">
    <div class="left">
        搜索结果列表 </div>
    <div class="right">
        约有 <strong style="color: red;">
            <%=search.totalRecord%></strong> 项符合 "<span style="color: red;"><%=keyWord%></span>"
        的查询结果<%if (projectList.Count() != 0)
               {%>，以下是第<strong style="color: red;"><%=startIndex%>-<%=endIndex%></strong> 项<%}%></div>
</div>
<div class="structlistmain">
    <table width="100%">
        <tr>
            <td valign="top">
                <%foreach (var project in projectList)
                  {
                      string path = "";
                      var file = dataOp.FindAllByQueryStr("FileRelation", "tableName=XH_DesignManage_Project&&isCover=true&fileObjId=28&keyValue=" + project.Text("projId")).OrderByDescending(t => DateTime.Parse(t.Text("createDate"))).FirstOrDefault();
                      if (file != null)
                      {
                          var fileModel = dataOp.FindOneByKeyVal("FileLibrary", "fileId", file.Text("fileId"));
                          path = fileModel.Text("thumbPicPath");
                      }
                      var engObj=project.SourceBson("engId");
                %>
                <div class="SearchResults ">
                    <div class="title">
                        <a href="/DesignManage/ProjectIndex?projId=<%=project.Text("projId")%>" target="_blank">
                            <%if(!String.IsNullOrEmpty(keyWord)){%><%=project.Text("name").Replace(keyWord,"<span>"+keyWord+"</span>")%><%}else{%><%=project.Text("name") %><%}%></a></div>
                    <table width="100%">
                        <tr>
                            <td width="130" valign="top">
                                <div class="picdivm">

                                <%if (file != null)
                                  { %>
                                 <a href="javascript:void(0);" onclick='<%=FileCommonOperation.GetClientOnlineRead(file) %>'>
                                    <img style="border: 0px;" src="<%=path %>"
                                        alt="<%=file.Text("name")%>" text="<%=file.Text("name")%>" /></a>
                                    <%}
                                  else
                                  {%>
                                  <a>
                                    <img style="border: 0px;" src="<%=SysAppConfig.HostDomain %>/Content/images/Docutype/default_m.png"
                                        alt="暂无图片" text="" /></a>
                                    <%} %>
                                </div>
                            </td>
                            <td valign="top">
                                所属城市：<%=engObj!=null?engObj.SourceBsonField("city","name"):string.Empty%><br />
                                所属区域：<%=project.SourceBsonField("areaId","name")%><br />
                                预估售价:<%=project.Text("SellingPrice")%><br />
                                 <div class="Attached">
                                    创建人：<%=project.CreateUserName()%>
                                    创建时间：<%=project.CreateDate() %></div>
                               
                            </td>
                        </tr>
                    </table>
                </div>
                <%}%>
            </td>
        </tr>
    </table>
</div>
<div id="pag"></div>
<script type="text/javascript">
    $(function() {
        var current = <%=current %>;
        $("#pag").pagination({ totalPage: <%=totalPage %>, display_pc: 7, current_page:current,showPageList:false,showRefresh:false,displayMsg:'',
            onSelectPage: function(current_page, pagesize) {
                var keyWord = "<%=keyWord %>";
                var provinceId = "<%=provinceId %>";
                var cityId = "<%=cityId %>";
              
                var url = "/SearchEngine/SearchProjectControl?keyWord=" + escape(keyWord) + "&provinceId=" + provinceId + "&cityId=" + cityId + "&r=" + Math.random() + "&current=" + current_page;
                $("#divResultList").load(url);
            }
        });
    });
</script>
