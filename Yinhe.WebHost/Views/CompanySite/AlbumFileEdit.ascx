<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Document" %>
<% 
   var fileId = PageReq.GetParam("fileId");
    var albumId = PageReq.GetParam("albumId");
    var compObj = dataOp.FindOneByKeyVal("CompanyAlbumFile", "fileId", fileId.ToString());
    var queryStr = string.Empty;
    if (compObj.Int("fileId")!=0)
    {
          queryStr = "db.CompanyAlbumFile.distinct('_id',{'fileId':'" + fileId.ToString() + "'})";
      
    }
    else
    {
        compObj = new BsonDocument();
    }
   
%>
 
   
    <div class="Editcontent">
    <div  <%if(compObj.Int("fileId")==0){ %> style=" display:none" <%} %>>
     <form action="" name="companyForm" action="" id="companyForm" method="post">
    <input type="hidden" name="fileId" value="<%=compObj.Text("fileId")%>" />
     <input type="hidden" name="albumId" value="<%=albumId%>" />
    <input type="hidden" name="tbName" value="CompanyAlbumFile" />
    <input type="hidden" name="queryStr" value="<%=queryStr %>" />
        <table>


            <tr>
               <td width="70" height="35">
                    名称：
                </td>
                <td width="250">
                    <input type="text" name="fileName" class="inputborder"   style="width: 150px"
                       value="<%=compObj.String("fileName") %>" />
                </td>
                <td width="70">
                    时间：
                </td>
                <td>
                   <input type="text" name="buildDate" class="inputgreenbgCursor" style="width: 150px" readonly
                        value="<%=compObj.String("buildDate") %>"  onclick="WdatePicker({dateFmt:'yyyy-MM-dd'})"  />
                </td>
            </tr>
            <tr>
                <td height="35" valign="top" style=" padding-top:8px;*padding-top:13px">
                    详细内容：
                </td>
                <td colspan="3"  valign="top" style=" padding-top:8px">
                    <textarea  name="remark" rows="6" cols="57" class="wordwrap"><%=compObj.String("remark")%></textarea>
               </td>
             </tr>
            
             
        </table>    </form>
       </div>
       <table>
     <%if (compObj.Int("fileId") != 0)
       { %>
    <tr>
          <td width="70" height="35" valign="top" style=" padding-top:8px;*padding-top:13px">
                    图片：
                </td>
                <td  valign="top" style=" padding-top:8px">
                     <input type="text" name="name" class="inputborder"   style="width: 250px"
                       value="<%=compObj.String("ImagePath") %>" />
               </td>
             </tr>
     <%} %>
      <tr>
           <td></td>
                <td  valign="top" style=" padding-top:8px">
                  <input type="file" id="file" name="file"/>  
                  <input type="hidden" id="flag" name="flag" value="ajax文件上传"/> 
                  <input type="button" onclick="ajaxFileUpload('<%=compObj.String("fileId") %>','<%=albumId %>')" id="btnUpload" value="上传图片" />
               </td>
             </tr>
    </table>
    </div>

    

   
 
 
 
 
