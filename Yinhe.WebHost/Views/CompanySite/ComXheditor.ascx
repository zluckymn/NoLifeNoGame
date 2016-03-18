<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
  
  <% 
   
   var tableName = PageReq.GetParam("tableName");
   var referField = PageReq.GetParam("referField");
   var referValue = PageReq.GetParam("referValue");
   var htmlField = PageReq.GetParam("htmlField");
    BsonDocument compObj=null;
    if (string.IsNullOrEmpty(htmlField))
    {
        htmlField = "cooperationClient";
    }
   if (string.IsNullOrEmpty(tableName))
   {
       tableName = "CompanyInfo";
   }
   if (string.IsNullOrEmpty(referField))
   {
       referField = "compId";
   }
   if (string.IsNullOrEmpty(referValue))
   {
       compObj = dataOp.FindAll(tableName).Where(c=>c.Text("type")=="1").FirstOrDefault();
       if (compObj != null)
       {
           referValue = compObj.Text(referField);
       }
       else
       {
           referValue = "1";
       }
   }
   if (compObj == null)
   {
       compObj = dataOp.FindOneByKeyVal(tableName, referField, referValue);
   }

    var compId = referValue;
    var queryStr = string.Empty;
    if (compObj != null && compObj.Int(referField)!=0)
    {
         compId = compObj.Text(referField);
         queryStr = "db.CompanyInfo.distinct('_id',{'compId':'" + compId.ToString() + "'})";
         var subStr = "{'" + referField + "':'" + compId + "'}";
         queryStr = string.Format("db.{0}.distinct('_id',{1})", tableName, subStr);
    }
    else
    {
        compObj = new BsonDocument();
    }

   
%>

    <div >
       
        <div >
        
            <div >
                <div id="xheditorDiv">
                   <textarea id="cooperationClient" class="xheditor" name="cooperationClient" style="width:100%; height:500px;"><%=compObj.Text(htmlField)%></textarea>
                </div>
                <br />
                <div class="btn">
                    <a class="sign_in_button_2" href="javascript:;" onclick="saveFeedBackRecord();">提交<span></span>
                    </a>&nbsp;<a href="javascript:void(0);" class="sign_in_button_3" onclick="sm_showIdea()">取消</a></div>
                <p>
               
                </p>
            </div>
           
        </div>
    </div>

    
    <script type="text/javascript">

      var edit=  $('#cooperationClient').xheditor({ upLinkUrl: '/CompanySite/SaveImageCompanySite?immediate=1', upImgUrl: '/CompanySite/SaveImageCompanySite?immediate=1', upFlashUrl: '/CompanySite/SaveImageCompanySite?immediate=1', upMediaUrl: '/CompanySite/SaveImageCompanySite?immediate=1' });
      edit.focus();
        function saveFeedBackRecord() {
           $.ajax({
                url: "/CompanySite/SaveCompanyInfo",
                type: "post",
                data: {
                    htmlEditField: "<%=htmlField %>",
                     htmlEditValue: escape(edit.getSource()),
                     tableName: "<%=tableName %>",
                     keyField: "<%=referField %>",
                    keyValue: "<%=referValue %>"
                },
                dataType: "json",
                error: function () {
                    $.tmsg("m_jfw", "未知错误，请联系服务器管理员，或者刷新页面重试", { infotype: 2 });
                },
                success: function (data) {
                    if (data.Success == false) {
                        alert(data.Message);
                    }
                    else {
                        alert("保存成功");
                        window.location.reload();
                    }
                }
            });
        }
    </script>
  