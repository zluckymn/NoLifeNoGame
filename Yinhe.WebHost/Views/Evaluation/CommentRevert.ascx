<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%
    //var result = (TodoTask)ViewData["object"];
    //ViewData["result"] = result;
    int type = PageReq.GetParamInt("type");
    int action = PageReq.GetParamInt("action");
    int comObjId = PageReq.GetParamInt("coid");
    int objId = PageReq.GetParamInt("id");
    var CommentsId = PageReq.GetParamInt("comentId");
    
 %>
 <script language="javascript">
    // var objName = "=result.name ";
    var objName = "";
 </script>
 
                     <div id="divCommentList"></div>
                     <script language="javascript">
                         $("#divCommentList").load("/Evaluation/RevertList?coid=<%=comObjId %>&id=<%=objId %>&comentId=<%=CommentsId %>&type=<%=type %>&action=<%=action %>&r=<%=DataTime.Now.Ticks %>")
                     </script>
       
   
    <div id="ShowAllHisVer" style="display: none; position: absolute" assign="box"  class="shadow-container">

    </div>
    <div id="CommentEdit" assign="box" style="display: none; position: absolute" class="shadow-container2">
    </div>
    <div id="RevertEdit" assign="box" style="display: none; position: absolute" class="shadow-container2">
    </div>
    <script language="javascript">
       
   </script>