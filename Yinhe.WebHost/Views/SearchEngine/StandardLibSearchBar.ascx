<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Permissions" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Common" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Document" %>
<div style="padding-top: 5px;">
<% var standarResultLibTable = "XH_StandardResult_StandardResultLibrary";
   var libList  = dataOp.FindAll(standarResultLibTable).ToList();
 
    %>
 
    <%   
        var firstLib = libList.FirstOrDefault();
        foreach (var tempLib in libList)
        { %>
    <% if (libList.Count() > 1)
       { %>
    <input name="libRadios" type="radio" <%if(firstLib.Text("libId")==tempLib.Text("libId")){ %>checked<%} %> value="<%=tempLib.Text("libId") %>"   onclick="changeLib(<%=tempLib.Text("libId") %>);" />
        <%=tempLib.Text("name") %>&nbsp;&nbsp;
    <%} %>
    <%  } %>
    <input id="checkedLibIds" type="hidden" value="<%=firstLib!=null?firstLib.Text("libId"):"" %>"/>
</div>
<!-----------------------------------------------------------分割线,以下是各个库不同的搜索条----------------------------------------------------------->
 
<!--初始化-->
<script type="text/javascript">
    function changeLib(id) {
        $("div[name=libShowDiv]").hide();
        $("#libSearchBar_" + id).show();
        $("#checkedLibIds").val(id);
       
    }
    var libIds = "";
    //pathType,name,mathType,value|Y|
    var xmlKeyStr = "";
    var retStr = "";
    var splitStr = "|Y|";

    function generateCondition() {
        var x = 0;
        var div = $("div[id^=libSearchBar_]:visible");
        div.find("input[searchtype=xml]").each(function (i) {
            x++;
            var v = $(this).val();
            if ($(this).attr("val")) { v = div.find("input[name=" + $(this).attr("val") + "]").val(); }

            if (x == div.find("input[searchtype=xml]").length) {
                xmlKeyStr += MadeXmlStr($(this), v, true);
            } else {
                xmlKeyStr += MadeXmlStr($(this), v);
            }
        });
        div.find("select[searchtype=param]").each(function () {
            if (retStr != "") retStr += "&";
            retStr += $(this).attr("name") + "=" + $(this).val();
        });
        div.find("input[type=checkbox][searchtype=param]").each(function () {
            if ($(this).attr("searched") != "true") {
                var t = div.find("input[type=checkbox][name=" + $(this).attr("name") + "]:checked").val();
                if (t != "") {
                    if (retStr != "") retStr += "&";
                    t = "";
                    div.find("input[type=checkbox][name=" + $(this).attr("name") + "]:checked").each(function () {
                        if (t != "") t += ",";
                        t += $(this).val();
                    });
                    retStr += $(this).attr("name") + "=" + t;
                }
                div.find("input[type=checkbox][name=" + $(this).attr("name") + "]").attr("searched", "true");
            }
        });
        div.find("input[searched=true]").attr("searched", "");

        var conditionStr = "&libIds=" + libIds + "&curLibIds=" + $("#checkedLibIds").val() + "&" + retStr + "&xmlKeyStr=" + escape(xmlKeyStr) + "";

        //alert(conditionStr);

        if ($("#resultCondition")) {
            $("#resultCondition").val(conditionStr);
        }

        //window.open("/SearchEngine/Home/StandardSearchRetList?keyWord=" + conditionStr);

        xmlKeyStr = "";
        retStr = "";
    }

    function MadeXmlStr(o, val, isEnd) {
        var mathType = o.attr("mathtype");
        var pathType = o.attr("pathtype");
        var name = o.attr("name");
        if (val != "") {
            if (isEnd) { return pathType + "," + name + "," + mathType + "," + val; }
            else {
                return pathType + "," + name + "," + mathType + "," + val + splitStr;
            }
        } else {
            return "";
        }
    }
   
</script>
 