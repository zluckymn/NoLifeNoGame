<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<link href="<%=SysAppConfig.HostDomain %>/Content/css/client/QX/qiaoxin.css" rel="stylesheet"
        type="text/css" />
<link href="<%=SysAppConfig.HostDomain %>/Content/css/common/feedback.css" rel="stylesheet" type="text/css" />
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Reference/Common/jquery-1.5.1.js"
    type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/Common/YH.js" type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/Common/jquery.bgiframe.js"
    type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/Common/popbox.js" type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/Common/CommonFunc.js"
    type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/Common/Yinhoo.SelelctTree.js"
    type="text/javascript"></script>
<%--文档下载引用--%>
<script type="text/javascript">
    function Fs() {
        this.PopDownLimitLength = 999;
    }

    var fs = new Fs();
</script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Reference/myFocus/js/myfocus-1.2.0.full.js"
    type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Reference/jQuery/Alerts/jquery.hiAlerts-min.js"
    type="text/javascript"></script>
<link href="<%=SysAppConfig.HostDomain %>/Scripts/Reference/jQuery/Alerts/jquery.hiAlerts.css"
    rel="stylesheet" type="text/css" />
<%--end--%>
<script type="text/javascript" src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/Common/YHLoader.js?m=pagination"></script>
<script src="<%=SysAppConfig.HostDomain %>/webMT/js/cwf.js" type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/webMT/js/client.js" type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/mt.js" type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/FileCommon.js" type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/Common/EditTable.js" type="text/javascript"></script>
<link href="<%=SysAppConfig.HostDomain %>/Scripts/Reference/highslide/highslide.css"
    rel="stylesheet" type="text/css" />

<script src="<%=SysAppConfig.HostDomain %>/Scripts/Reference/datePicker/WdatePicker.js"
    type="text/javascript"></script>
<script type="text/javascript" src="<%=SysAppConfig.HostDomain %>/Scripts/Reference/highslide/highslide-with-gallery.js"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/Common/SysMenuQX.js" type="text/javascript"></script>
<script type="text/javascript">
    window.onload = function () {

        hs.graphicsDir = '<%=SysAppConfig.HostDomain %>/Scripts/Reference/highslide/graphics/';
        hs.align = 'center';
        hs.registerOverlay({
            html: '<div class="closebutton" onclick="return hs.close(this)" title="Close"></div>',
            position: 'top right',
            fade: 2 // fading the semi-transparent overlay looks bad in IE
        });

        hs.transitions = ['expand', 'crossfade'];
        hs.outlineType = 'rounded-white';
        hs.fadeInOut = true;
        hs.numberPosition = 'caption';
    }
</script>
