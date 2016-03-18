<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<style type="text/css">
    .style1
    {
        width: 50px;
    }
</style>
<% 
    string type = PageReq.GetParam("type");
    int comObjId = PageReq.GetParamInt("coid");
    int objId = PageReq.GetParamInt("id");
    var order = PageReq.GetParam("type");
    var dataOp = new DataOperation();
    // var comments = dataOp.FindAllByKeyVal("Evaluation_Comment", "commentObjectId", comObjId.ToString()).ToList();
    var comments = dataOp.FindAllByQueryStr("Evaluation_Comment", string.Format("commentObjectId={0}&objectId={1}", comObjId, objId)).ToList();
    var userId = dataOp.GetCurrentUserId();//获取用户Id

   
%>
<%=Html.Hidden("comObjId", comObjId)%>
<%=Html.Hidden("objId", objId)%>
<%=Html.Hidden("type", type)%>
<%
    if (comments.Count > 0)
    {
        foreach (var comment in comments)
        {
            var userName = "";//获取用户名称
            var curUser = dataOp.FindOneByKeyVal("SysUser", "userId", comment.Text("updateUserId"));
            if (curUser != null)
            {
                userName = curUser.Text("name");

            }
            var commentReverts = dataOp.FindAllByKeyVal("Evaluation_CommentReply", "commentId", comment.Text("commentId"));
%>
<div style="padding-bottom: 10px;">
    <table width="100%">
        <tr>
            <td valign="top" class="style1">
                <img src="<%=SysAppConfig.HostDomain %>/Content/Images/Comments/pic10.jpg" />
            </td>
            <td valign="top">
                <div style="margin: 0px 8px 8px 8px">
                    <table width="100%" style="margin-bottom: 10px">
                        <tr bgcolor="#f2f2f2" style="padding: 0px 5px">
                            <td style="color: #016a9f" height="25">
                                <%=comment.Text("comTitle") %>
                            </td>
                            <td align="right">
                                <span style="cursor: pointer" onclick='$("#divCommentList").load("/Evaluation/CommentRevertList?coid=<%=comObjId %>&id=<%=objId %>&comentId=<%=comment.Text("commentId") %>&type=<%=type %>&action=0&r=" + Math.random());'>
                                    <img src="<%= SysAppConfig.HostDomain %>/Content/Images/Comments/ico25.jpg" width="14"
                                        height="12" />
                                    <font style="color: #0066cb; font-weight: bold">
                                        <%=commentReverts.Count()%>&nbsp;&nbsp;&nbsp;</font></span>
                                <%--    林少注释  2013.04.10--10:48
                                            <span style="padding-left: 5px">
                                            <img src="<%=SysAppConfig.HostDomain %>/Content/Images/Comments/ico26.jpg" width="10"
                                                height="10" />
                                            <font color="#FE3602">
                                                <%=!string.IsNullOrEmpty(comment.Text("commentNum"))?comment.Text("commentNum"):"0"%></font></span>
                                --%>
                            </td>
                        </tr>
                    </table>
                    <div style="line-height: 16px; color: #424242; padding: 5px 8px 10px 8px">
                        <%=StringExtension.CutStr(comment.Text("comContent"), 270, "...")%>
                        <a href="javascript:void(0)" class="blue" onclick='$("#divCommentList").load("/Evaluation/CommentRevertList?coid=<%=comObjId %>&id=<%=objId %>&comentId=<%=comment.Text("commentId") %>&type=<%=type %>&action=0&r=" + Math.random());'>
                            回复</a></div>
                    <div style="color: #A0A0A0; padding: 5px 8px 10px 8px">
                        <table width="100%">
                            <tr>
                                <td>
                                    类型: &nbsp;&nbsp;&nbsp;&nbsp;评价:&nbsp;&nbsp;<%=StringExtension.CutStr(userName, 10, "..")%>
                                </td>
                                <td align="right" width="140" class="nopadding">
                                    <img src="<%= SysAppConfig.HostDomain %>/Content/Images/Comments/ico27.jpg" width="10"
                                        height="9" />
                                    <%=comment.Text("createDate")%>
                                </td>
                            </tr>
                            <tr>
                                <td align="right" colspan="2" height="25">
                                    <a hidefocus="true" href="javascript:void(0)" class="edit" onclick="showCommentEdit('CommentEdit','/Evaluation/CommentEdit/?id=<%=comment.Text("commentId")%>&coid=<%=comObjId %>&r=<%=DateTime.Now.Ticks %>');">编辑</a>
                                    <a hidefocus="true" href="javascript:void(0)" class="delete" onclick="CustomDeleteItem(this,reload);"
                                        tbname="Evaluation_Comment" querystr="db.Evaluation_Comment.distinct('_id',{'commentId':'<%=comment.Int("commentId") %>'})">删除</a>
                                </td>
                            </tr>
                        </table>
                    </div>
                    <%if (commentReverts.Count() > 0)
                      {%>
                    <%var index1 = 1;
                      foreach (var revert in commentReverts)
                      {
                         
                    %>
                    <table width="100%" style="margin-bottom: 12px; margin-top: 10px">
                        <tr>
                            <%
                          var picturePath = "/Content/Images/Comments/pic10.jpg";
                          var user = dataOp.FindOneByKeyVal("SysUser", "userId", revert.Text("updateUserId"));
                          if (curUser != null)
                          {
                              if (!string.IsNullOrEmpty(curUser.Text("picturePath")))
                              {
                                  picturePath = curUser.Text("picturePath");
                              }
                          }
                             
                            %>
                            <td width="40" valign="top">
                                <%--<img src="<%=Yinhoo.Utilities.Core.Extensions.StringExtension.GetFileFullNameNoExt(picturePath) + "_us.jpg"%>"
                                onerror="setTypeImg(this,'us');" />--%>
                                <img src="<%=SysAppConfig.HostDomain %><%=picturePath%>" />
                            </td>
                            <td valign="top">
                                <table width="100%" style="border-bottom: 1px solid #E2E0E1; margin-bottom: 5px;
                                    line-height: 18px">
                                    <tr>
                                        <td>
                                            来自：<%=user.Text("name")%>&nbsp;&nbsp; <span style="color: #A49B9C">
                                                [<%=revert.Text("createDate")%>]
                                                <%if (revert.Text("createUserId") == userId.ToString())
                                                  {%><a hidefocus="true" href="javascript:void(0)" class="edit" onclick="showCommentEdit('RevertEdit','/Evaluation/RevertEdit/?id=<%=revert.Text("revertId")%>&coid=<%=comObjId %>&r=<%=DateTime.Now.Ticks %>');">编辑</a><%}%><%if (revert.Text("createUserId") == userId.ToString())
                                                                                                                                                                                                                                                                                 {%><a hidefocus="true" href="javascript:void(0)" class="delete" onclick="CustomDeleteItem(this,reload);"
                                                                                                                                                                                                                                                                                     tbname="Evaluation_CommentReply" querystr="db.Evaluation_CommentReply.distinct('_id',{'revertId':'<%=revert.Int("revertId") %>'})">删除</a><%}%>
                                            </span>
                                        </td>
                                        <td align="right" style="color: #939395">
                                            <%=index1%>楼
                                        </td>
                                    </tr>
                                </table>
                                <div style="line-height: 19px; padding: 5px 5px 10px 5px; color: #5E5E5E">
                                    <%=revert.Text("commentRec")%></div>
                            </td>
                        </tr>
                    </table>
                    <%index1++;
                      } %>
                    <%} %>
            </td>
        </tr>
    </table>
</div>
<%} %>
<%} %>
<div id="addComment" style="display: block; margin-bottom: 10px; padding-bottom: 10px;
    line-height: 30px">
    <%string updateQuery = "";
    %>
    <form id="commentForm" method="post">
    <input type="hidden" name="tbName" value="Evaluation_Comment" />
    <input type="hidden" name="queryStr" value="<%=updateQuery %>" />
    <input type="hidden" name="commentObjectId" value="<%=comObjId %>" />
    <input type="hidden" name="objectId" value="<%=objId %>" />
    <div style="margin: 0px 8px 8px 8px">
        <table width="100%">
            <tr>
                <td width="60" height="35">
                    标题：
                </td>
                <td>
                    <input type="text" name="comTitle" id="CommentTitle" class="inputborder" style="width: 95%;" />
                </td>
            </tr>
            <tr>
                <td valign="top">
                    内容：
                </td>
                <td>
                    <textarea name="comContent" rows="10" id="CommentDesc" class="textarea_01" cols="45"
                        rows="5" style="width: 95%;"></textarea>
                </td>
            </tr>
            <tr>
                <td height="50">
                    &nbsp;
                </td>
                <td>
                    <a class="btn_01" hidefocus="true" href="javascript:;" style="cursor: pointer" onclick="addCommentContent('<%=objId %>');">
                        提交评价<span></span></a>
                </td>
            </tr>
        </table>
    </div>
    </form>
</div>
<script type="text/javascript">
    $("#sp_objName").html("<%=objId %>");
    $("span[name=sp_commentObj]").each(function () {
        $(this).html("<%=objId %>");
    });
    //initSignalBox('CommentEdit<%=objId %>'); 
</script>
<input type="hidden" id="genreId" value="" />
<input type="hidden" id="action" value="<%=PageReq.GetParamInt("action")%>" />
<input type="hidden" id="ProjRetId" value="<%=objId %>" />
<script type="text/javascript">

    function Sort(type) {
        $("#divCommentList").load("/Evaluation/CommentList?coid=<%=comObjId %>&id=<%=objId %>&action=0&type=" + type + "&r=" + Math.random());
    }

    //添加任务书comment
    function addCommentContent(id) {
        if ($("#CommentDesc").val() == "") { top.showInfoBar("请填写评价内容"); return false; }

        var formdata = $("#commentForm").serialize();


        $.ajax({
            url: "/Home/SavePostInfo",
            type: 'post',
            data: formdata,
            dataType: 'json',
            error: function () {
                hiAlert('未知错误，请联系服务器管理员，或者刷新页面重试', '保存失败');
            },
            success: function (data) {
                if (data.Success == false) {
                    alert(data.Message);
                }
                else {
                    alert("保存成功");
                    $("#divCommentList").load($("#divCommentList").attr("url") + "&r=" + Math.random());
                }
            }
        });

    }

    function showCommentEdit(id, url) {

        var title = "";
        if (id == "CommentEdit") {
            title = "编辑评论";
        } else {
            title = "编辑回复";
        }
        box(url, { title: title, contentType: 'ajax', width: 400,
            onOpen: function (o) {

            },
            submit_cb: function (o) {
                if (id == "CommentEdit") {

                    saveInfo("#" + id);
                } else {
                    saveRevertEdit("#" + id, o);
                }
            }

        });
    }

    function reload() {
        $("#divCommentList").load($("#divCommentList").attr("url") + "&r=" + Math.random())
    }
</script>
