<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Permissions" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Common" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Document" %>
<% 
    
 
    var libId = PageReq.GetParamInt("curLibIds");
    var totalPage = 0;
    var keyWord = Uri.UnescapeDataString(PageReq.GetParam("strKeyWord"));
    
    Yinhe.ProcessingCenter.MvcFilters.CommonSearch search=null;
    if (libId != 0)
    {
        search = new Yinhe.ProcessingCenter.MvcFilters.CommonSearch("XH_StandardResult_StandardResult", string.Format("libId={0}", libId));
    }
    else
    {
        search = new Yinhe.ProcessingCenter.MvcFilters.CommonSearch("XH_StandardResult_StandardResult");
    }
    var current = PageReq.GetParamInt("current");
    if (current <= 0)
    {
        current = 1;
    }
    var keyFieldList = new List<string>();
    keyFieldList.Add("name");
    var retList = search.QuickSearch(current, out totalPage, keyFieldList, keyWord);
    //区域过滤
    if (libId != 0)
    {
        retList = retList.Where(c => c.Int("libId") == libId).ToList();
    }
  
    var startIndex = (current - 1) * SysAppConfig.PageSize + 1;
    var endIndex = current + retList.Count() - 1;
   
%>

 
<div class="structlisttop" style="margin-top: 30px;">
    <div class="left">
        搜索结果列表</div>
    <div class="right">
        约有 <strong style="color: red;">
            <%=search.totalRecord%></strong> 项符合 "<span style="color: red;"><%=keyWord %></span>"
        的查询结果<%if (retList.Count() != 0)
               {%>，以下是第<strong style="color: red;"><%=startIndex%>-<%=endIndex%></strong> 项<%}%></div>
</div>
<div class="structlistmain">
    <table width="100%">
        <tr>
            <td valign="top">
                <%  foreach (var tempRet in retList)
                    {
                        var retId = tempRet.Text("retId");
                        var retName = tempRet.Text("name");
                        var stdRetType = dataOp.FindOneByKeyVal("XH_StandardResult_ResultType", "typeId", tempRet.Text("typeId"));
                        var curtype = string.Empty;
                        var typeName = string.Empty;
                        if (stdRetType != null)
                        {
                            curtype = stdRetType.Text("type");
                            typeName = stdRetType.Text("name");
                        }
                        var linkUrl = "/StandardResult/UnitView/";
                        switch (curtype)
                        {   //户型
                            case "1":
                                linkUrl = "/StandardResult/UnitView/"; break;
                            //住宅标准层
                            case "2":
                                linkUrl = "/StandardResult/ResidentialStandardView/"; break;
                            //别墅户型 
                            case "3":
                                linkUrl = "/StandardResult/VillaUnitView/"; break;
                            //别墅单元
                            case "4":
                                linkUrl = "/StandardResult/VillaUnitView/"; break;
                            //房型
                            case "5":
                                linkUrl = "/StandardResult/UnitView/"; break;
                            //室内精装修
                            case "6":
                                linkUrl = "/StandardResult/FineDecorationView/"; break;
                            //立面 
                            case "7":
                                linkUrl = "/StandardResult/FacadeView/"; break;
                            //示范区
                            case "8":
                                linkUrl = "/StandardResult/DemonstrationAreaView/"; break;
                            //景观
                            case "9":
                                linkUrl = "/StandardResult/LandscapeView/"; break;
                            //标准工艺工法
                            case "10":
                                linkUrl = "/StandardResult/PEMView/"; break;
                            //公共部位
                            case "11":
                                linkUrl = "/StandardResult/PublicPartsView/"; break;
                            //设备与技术
                            case "12":
                                linkUrl = "/StandardResult/EquipmentView/"; break;
                            //绿化景观
                            case "13":
                                linkUrl = "/StandardResult/GreenLandscapeView/"; break;
                        }
                        linkUrl += "?retId=" + retId;
                        //成果封面图
                        BsonDocument linethumbPicPath = dataOp.FindOneByQueryStr("FileRelation", "tableName=XH_StandardResult_StandardResult&fileObjId=10&keyValue=" + retId.ToString());
                        BsonDocument fileEntity = linethumbPicPath.SourceBson("fileId");
                       
                %>
                <div class="SearchResults ">
                    <div class="title">
                        <a href="<%=linkUrl %>" target="_blank">
                            <%=tempRet.Text("name") %></a></div>
                    <table width="100%">
                        <tr>
                            <td width="130" valign="top">
                                <div class="picdivm">
                                    <a href="javascript:;" class="a_img_box" onclick="window.location.href='<%=linkUrl %>'"
                                    onmouseover="ImgView(this);" onmouseout="ImgMiss(this);">
                                    <img src="<%=fileEntity.String("thumbPicPath").Replace("_m.","_hm.") %>" /></a>
                                </div>
                            </td>
                            <td valign="top">
                                <div class="Synopsis">
                                </div>
                                所属库：<%=tempRet.SourceBsonField("libId", "name")%><br />
                                所属类型：<%=tempRet.SourceBsonField("typeId", "name")%><br />
                                所属目录：<%=tempRet.SourceBsonField("catId", "name")%><br />
                                所属区域：<%=tempRet.SourceBsonField("areaId", "name")%><br />
                                <div class="Attached">
                                    创建人：<%=tempRet.CreateUserName()%>
                                    创建时间：<%=tempRet.CreateDate() %></div>
                            </td>
                        </tr>
                    </table>
                </div>
                <%} %>
            </td>
        </tr>
    </table>
</div>
<div id="pag">
</div>

<script type="text/javascript">
    $(function () {
        var current = parseInt("<%=current %>");
        $("#pag").pagination({ totalPage: parseInt("<%=totalPage %>"), display_pc: 7, current_page: current, showPageList: false, showRefresh: false, displayMsg: '',
            onSelectPage: function (current_page, pagesize) {
                var keyWord = "<%=keyWord %>";
                var url = "/SearchEngine/StandardSearchRetList?keyWord=" + escape(keyWord) + "&curLibIds=<%=libId %>&r=" + Math.random() + "&current=" + current_page;
                $("#divResultList").load(url);
            }
        });
    });
        
        $(document).ready(function () {
            //$("div.picLibrary:first").find("div.fr").find("img").click();
        });
        function ImgView(obj) {
            $(obj).parent().parent().parent().find(".floating02").fadeIn().end();
        }
        function ImgMiss() {
            $(".floating02").hide();
        }


        function showHideDiv(obj, src) {
            $(obj).parents("div.picLibrary").find("div.box_con").slideToggle('slow', function () {
                if ($(obj).parents("div.picLibrary").find("div.box_con").css("display") == "none") {
                    $(obj).attr("src", "<%=SysAppConfig.HostDomain %>/Content/images/icon/ico0002.png");

                } else {
                    $(obj).attr("src", "<%=SysAppConfig.HostDomain %>/Content/images/icon/ico0001.png");
                    //全部显示
                    $(obj).parents("div.trow").siblings().find("div.trow-body").slideUp("slow", function () {
                        if ($(this).css("display") == "none") {
                            $(this).parent().find(".tr_hover").find("img").attr("src", src + "/Content/images/icon/ico0003.gif");
                        }
                    });

                }
            });
        }

        $(document).ready(function () {
            SetMenu(4, 0);
        });  
  

</script>

