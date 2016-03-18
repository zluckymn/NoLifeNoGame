<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Document" %>
<% 
   var albumId = PageReq.GetParam("albumId");
     var compObj = dataOp.FindOneByKeyVal("CompanyAlbum", "albumId", albumId.ToString());
    var queryStr = string.Empty;
    if (compObj.Int("albumId")!=0)
    {
          queryStr = "db.CompanyAlbum.distinct('_id',{'albumId':'" + albumId.ToString() + "'})";
      
    }
    else
    {
        compObj = new BsonDocument();
    }
   
%>
<div>
    <h2>
        基本信息</h2>
    <hr />
     <form action="" name="companyForm" action="" id="companyForm" method="post">
    <input type="hidden" name="albumId" value="<%=compObj.Text("albumId")%>" />
    <input type="hidden" name="tbName" value="CompanyAlbum" />
    <input type="hidden" name="queryStr" value="<%=queryStr %>" />
    <div class="Editcontent">
        <table>
            <tr>
                <td width="70" height="35">
                    名称：
                </td>
                <td width="250">
                    <input type="text" name="name" class="inputborder"   style="width: 150px"
                       value="<%=compObj.String("name") %>" />
                </td>
                   <td width="70" height="35">
                    时间：
                </td>
                <td width="250">
                  <input type="text" name="buildDate" class="inputgreenbgCursor" style="width: 150px" readonly
                        value="<%=compObj.String("buildDate") %>"  onclick="WdatePicker({dateFmt:'yyyy-MM-dd'})"  />
                </td>
               
            </tr>
         
             <tr>
                <td height="35" valign="top" style=" padding-top:8px;*padding-top:13px">
                    备注说明：
                </td>
                <td colspan="3"  valign="top" style=" padding-top:8px">
                    <textarea  name="remark" rows="6" cols="57" class="wordwrap"><%=compObj.String("remark")%></textarea>
               </td>
             </tr>
        </table>
    </div>
    </form>
</div>
  <a class="sign_in_button_2" onclick="SaveCompany()" href="javascript:;">保存<span></span></a>
  
  <br />
  <br /><br />


<%if (compObj.Int("albumId") != 0)
  {

      ///获取实践列表
      var fileList = dataOp.FindAllByKeyVal("CompanyAlbumFile", "albumId", albumId.ToString());
       %>
      <div>
       <h2>
        相册图片列表</h2>
    <hr />
    <table class="tableborder" id="segmentConcerns">
        <tr class="tableborder-title">
           
            <td width="200">
                图片
            </td>
             <td width="250">
               名称
            </td>
              <td width="250">
                描述
            </td>
             <td width="100" align="center">
                操作
            </td>
        </tr>
      
        <%foreach (var practice in fileList)
          {
              var practiceQueryStr = "db.CompanyAlbumFile.distinct('_id',{'fileId':'" + practice.String("fileId") + "'})";
              %>
        <tr>
         <td class="edit">
                <div class="tb_con"><img src="<%=practice.String("ImagePath")%>" style="width:100%" /></div>
              
            </td>
            <td class="edit">
                <div class="tb_con"><%=practice.String("fileName")%></div>
              
            </td>
            
            <td class="edit">
                <div class="tb_con"><%=practice.String("remark")%></div>
              
            </td>
            
            <td align="center">
               <a href="javascript:;" class="blue" onclick="editPractice(<%=practice.Text("fileId") %>)" >编辑</a>
                <a href="javascript:;" class="red" onclick="CustomDeleteItem(this,ReloadDiv,1)" tbName="CompanyAlbumFile" queryStr="<%=practiceQueryStr%>">删除</a>
            </td>
        </tr>
        <%}%>
    </table>
    <div style="padding-top: 5px;">
        <a href="javascript:;" class="sign_in_button_2" onclick="editPractice(0)">新增</a>
    </div>
    </div>
  
<%} %>


 
<script type="text/javascript">
    $(document).ready(function () {
 
    });

    
    function editPractice(fileId) {
        var url = "";
        url = "/CompanySite/AlbumFileEdit?fileId=" + fileId + "&albumId=<%= albumId%>";
        if (fileId != 0)
            url == "/CompanySite/AlbumFileEdit?fileId=" + fileId + "&albumId=<%= albumId%>";


        box(url, { boxid: 'AlbumFileEdit', title: '编辑', width: 600, contentType: 'ajax',
            submit_cb: function (o) {
                saveInfo(o.fbox.find("form"));
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
                       ReloadDiv();
                }
            }
        });
    }



    function ajaxFileUpload(fileId, albumId) {

        var data = { tableName: 'CompanyAlbumFile', keyValue: fileId, keyField: 'fileId', fieldName: "ImagePath", needCreate: "1", fkeyValue: albumId, fkeyField: 'albumId' };
        var elementIds = ["flag"]; //flag为id、name属性名
        $.ajaxFileUpload({
            url: '/CompanySite/SaveAjaxImage',
            type: 'post',
            data: data,
            secureuri: false, //一般设置为false
            fileElementId: 'file', // 上传文件的id、name属性名
            dataType: 'Text', //返回值类型，一般设置为json、application/json
            elementIds: elementIds, //传递参数到服务器
            success: function (data, status) {
                $.tmsg("m_jfw", "保存成功！", { infotype: 1 });
               closeById('AlbumFileEdit');
                ReloadDiv();
            },
            error: function (data, status, e) {
                alert(status)
                ReloadDiv();
            }
        });
        ReloadDiv();
        //return false;
    }

    function SaveCompany(obj) {
        var formData = $("#companyForm").serialize();
        $.ajax({
            url: '/Home/SavePostInfo',
            type: 'post',
            data: formData,
            dataType: 'json',
            error: function () {
                $.tmsg("m_jfw", "未知错误，请联系服务器管理员，或者刷新页面重试", { infotype: 2 });
            },
            success: function (data) {
                if (data.Success == false) {
                    $.tmsg("m_jfw", data.Message, { infotype: 2 });

                }
                else {
                    $.tmsg("m_jfw", "保存成功", { infotype: 1 });
                    ReloadDiv();
                }
            }
        });
    }

 
</script>
