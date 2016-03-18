<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Permissions" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Common" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Document" %>
<%
    int check = 0;
     var cityList = dataOp.FindAll("XH_ProductDev_City");
    var cityArea = dataOp.FindAll("XH_ProductDev_CityArea");
    var projList = dataOp.FindAll("XH_DesignManage_Project");
%>
<div style="padding-top: 5px;" >
    <input name="projectCondition" type="radio" value="" <%if(check==0){%>checked="checked"
        <%}%> onclick="changeSelect(0);" />
    项目库&nbsp;&nbsp;
    <input name="projectCondition" type="radio" value="" <%if(check==1){%>checked="checked"
        <%}%> onclick="changeSelect(1);" />
    项目图文档&nbsp;&nbsp;
<%--    <input name="projectCondition" type="radio" value="" <%if(check==2){%>checked="checked"
        <%}%> onclick="changeSelect(2);" />
    项目目录&nbsp;&nbsp;
--%>    
    </div>
<div style="padding-top: 15px;" id="divSearchProject" style="display:none" >
    <table>
        <tr>
            <td height="25">
               所属区域：
            </td>
            <td>
                <select name="areaId" id="areaId">
                <option value"-1">无</option>
                <%foreach (var area in cityArea)
                  {%>
                <option value="<%=area.Text("areaId") %>"><%= area.Text("name")%></option>
                <%} %>
                </select>
            </td>
            <td width="80" align="right">
                城市：
            </td>
            <td>
            <select  id="cityId">
                <option value"-1">无</option>
                <%foreach(var city in cityList) {%>
                <option value="<%=city.Text("cityId") %>"><%= city.Text("name")%></option>
                <%} %>
                </select>
            </td>
        </tr>
    </table>
</div>
<div style="padding-top: 15px;" id="divSearchProjectDoc" style="display:none" >
    <table>
        <tr >
            <td height="25" >
                所属项目：
            </td>
            <td >
                <select name="projId" id="projId">
                <option value"-1">无</option>
                <%foreach (var project  in projList)
                  {%>
                <option value"<%=project.Text("projId") %>"><%= project.Text("name")%></option>
                <%} %>
                </select>
            </td>
            
        </tr>
    </table>
</div>
<!--初始化-->

<script type="text/javascript">
    var check = "<%=check %>";

    function changeSelect(obj) {
        if (obj == 0) {
            check = "0";
            $("#divSearchProjectDoc").hide();
            $("#divSearchProject").show();

            $("#resultUrl").val("/SearchEngine/SearchProjectControl");
        }
        else if (obj == 1) {
            check = "1";
            $("#divSearchProject").hide();
            $("#divSearchProjectDoc").show();
            $("#divSearchProjectDoc").find("table").find("tr:eq(0)").find(".padding_a").show();
            $("#divSearchProjectDoc").find("table").find("tr:eq(1)").show();
            $("#resultUrl").val("/SearchEngine/SearchCrossProjDocumentControl");
        }
        else if (obj == 2) {
            check = "2";
            $("#divSearchProject").hide();
            $("#divSearchProjectDoc").show();
            //alert($("#divSearchProjectDoc").find("table").find("tr:eq(0)").find(".padding_a").html());
            $("#divSearchProjectDoc").find("table").find("tr:eq(0)").find(".padding_a").hide();
            $("#divSearchProjectDoc").find("table").find("tr:eq(1)").hide();
            $("#resultUrl").val("/SearchEngine/SearchProjDicControl");
        }
    }
    if (check == "1" || check == "2" || check == "0") {
        changeSelect(check);
    }
    function generateCondition() {
        var conditon = "";
        conditon += "&check=" + check;
        if (check == "0") {
//          conditon += "&orgId=" + $("#orgId").val();
            conditon += "&provinceId=" + $("#provinceId").val();
            conditon += "&cityId=" + $("#cityId").val();
        }
        else if (check == "1") {
            conditon += "&projId=" + $("#projId").val();
//            conditon += "&sysProfId=" + $("#sysProfId").val();
//            conditon += "&sysStageId=" + $("#sysStageId").val();
//            conditon += "&fileCatId=" + $("#fileCatId").val();
        }
        else if (check == "2") {
            conditon += "&projId=" + $("#projId").val();
        }
        $("#resultCondition").val(conditon);
    }
</script>

 