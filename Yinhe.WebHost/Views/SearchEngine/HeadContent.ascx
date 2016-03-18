<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<meta http-equiv="X-UA-Compatible" content="IE=EmulateIE7" />


<link href="<%=SysAppConfig.HostDomain %>/Content/css/client/ZHHY/zhonghai.css" rel="stylesheet"
    type="text/css" />
<link href="<%=SysAppConfig.HostDomain %>/Content/css/client/ZHHY/productDevelop.css"
    rel="stylesheet" type="text/css" />
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Reference/Common/jquery-1.5.1.js"
    type="text/javascript"></script>
<script type="text/javascript">
    //图片切换path地址
    var path = '<%=SysAppConfig.HostDomain %>';
</script>
    <script type="text/javascript" src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/Common/YHLoader.js?m=pagination"></script> 
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/Common/YH.js" type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/Common/jquery.bgiframe.js"
    type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/Common/popbox.js" type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/Common/CommonFunc.js"
    type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/Common/Yinhoo.SelelctTree.js"
    type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/reference/myFocus/js/myfocus-1.2.0.full.js"
    type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/webMT/js/cwf.js" type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/webMT/js/client.js" type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/mt.js" type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/FileCommon.js" type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/Common/EditTable.js" type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/YHFlexPaper/js/swfobject/swfobject.js"
    type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Reference/datePicker/WdatePicker.js"
    type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Reference/highslide/highslide.js"
    type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Reference/highslide/highslide-full-new.js"
    type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Reference/highslide/highslide-with-html-new.js"
    type="text/javascript"></script>
<script type="text/javascript" src="<%=SysAppConfig.HostDomain %>/Scripts/Reference/highslide/highslide-with-gallery.js"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Reference/highslide/highslide-with-html.js"
    type="text/javascript"></script>
<script type="text/javascript">
    window.onload = function () {
        hs.graphicsDir = '<%=SysAppConfig.HostDomain %>/Scripts/Reference/highslide/graphics/';
        hs.outlineType = 'rounded-white';
        hs.wrapperClassName = 'draggable-header';

        //hsNew 是新版本的highSlide
        hsNew.graphicsDir = '<%=SysAppConfig.HostDomain %>/Scripts/Reference/highslide/graphics/';
        hsNew.outlineType = 'rounded-white';
        hsNew.wrapperClassName = 'draggable-header';
    }
</script>
