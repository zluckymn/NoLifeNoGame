<%@ Page Language="C#" Inherits="Yinhe.ProcessingCenter.ViewPageBase" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>ViewDwgFile</title>
    <script type="text/javascript">
       
    </script>
</head>
<body>
    <%
        string filePath = ViewData["filePath"] as string;
        string domain = SysAppConfig.HostDomain;
     %>
    <div>
    <%--<div style=" text-align:right;"><input type="button" onclick="drawstampimp();" value="盖图章" /> </div>--%>
      <object id="dwgClient" classid="clsid:6EEC44E0-338B-408A-983E-B43E6F22B929" codebase="<%=domain %>/DwgControl/MxDrawX.CAB#version=1,0,0,1" width=100% height=96% align="left"> 
      <param name="_Version" value="65536" />
      <param name="_ExtentX" value="24262" />
      <param name="_ExtentY" value="16219" />
      <param name="_StockProps" value="0" />
      <param name="DwgFilePath" value="<%=filePath %>"  />
      <param name="IsRuningAtIE" value="1" />
      <param name="EnablePrintCmd" value="1" />
      <param name="ShowCommandWindow" value="0" />
      <param name="IniFilePath" value="EnableOleShow=Y">
      <param name="ShowToolBars" value="1" />
      <param name="ToolBarFiles" value="empty.mxt" />
      <param name="UserName" value="陈晓瀚" />
      <param name="UserCorporation" value="南京银禾企业管理咨询有限公司" />
      <param name="UserPhone" value="18906006076" />
      <param name="UserData" value="HHHH262AB346CB4FB33268F82C067BD80C0998015277215224C6E03D351C6B1F4FF6D0CCFAAB74A443313FB60000262A8A19635491E19896970C3A10A8749CB46251FF3E69FEFB5D77BF63B08CC5382867124CC96A4B0B670000262A9361B5B529FA5A4CC41E75241ECD86DAF348E385450E7EEC223D3E03D3E7B4E2B9DE270F6CE8904D0000262A7777C75F1B3E9E117AB9CE7ABDD32EA4E660FE67B42596D49C4F29ECB1A99430982AD638E66C0FBD0000262AF8FBDF1E987E466F15A2A56104C91B814373C3A0464586CA6884E9881D6B7BE7AFBDA15C036242920000262A3B5079FE5605EFB1B86E43D3F80C40DC487BEE598D1A15FDB9315000B82F8972BE0D060D476C53620000262A15222129828488D0192028CDA71AA1EC44CE967F79337916BBD71D198AD472B63C8FBCBF2BE6DC530000262A3598504A3177C8B6C0E307F6F4A00BD074CD5AF3205E910DAE113C367107F0B29F3E7B165491EB520000262A012CCC8F6F37FE5358082EF1569BA38DF5B08E27813D9DB5CFB72897C596B58D4C321F890057278A0000262A16B221CE2FEF9D839F93D501984727FC649F594719A219F6B41E393808F0D17A780B86B2D54347E00000040A5BF49063A436400D0000"  />
    </object>
   </div>
   
   <script type="text/javascript">
       var url = "http://" + window.location.host;
      
        //判断是否安装控件
       function DetectActiveX() {
           try {
               var comActiveX = new ActiveXObject("MXDRAWX.MxDrawXCtrl.1");
           }
           catch (e) {
               return false;
           }
           return true;
       }

        //加载工具条：服务器要配置mxt的mime
       function DoCustomEventEventFun(sEventName) {
           if (sEventName == "MxDrawXInitComplete") {
               var MxDrawXCtrl_Obj = document.getElementById("dwgClient");
               var mxtPath = url + "/mxt/toolbar.mxt";
               
               MxDrawXCtrl_Obj.LoadToolBar(mxtPath, 1);
              
               ZoomE();
               
           } 
       }
       
       
       if (DetectActiveX()) {
           document.getElementById("dwgClient").ImplementCustomEvent = DoCustomEventEventFun
       }
       else {
           alert("亲，你没有安装DWG浏览器");
       }

       function PageInit() { }
       function ZoomE() {
           

       }


       function drawstampimp() {
           var mxOcx = document.all.item("dwgClient");
           mxOcx.focus();
           var point1 = mxOcx.GetPoint(null, "\n 点取插入点:");
           if (point1 == null) {
               return;
           }
           var insPt = "" + point1.x + "," + point1.y + "";
           alert(insPt);
           document.getElementById("dwgClient").CallCustomFunction("MxET_DrawStamp", "\""+url+"/mxt/out.jpg\"," + insPt + ",1,\"stamplayer\"");
       }
   </script>
</body>
</html>
