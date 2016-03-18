var ExcelHelper = {
    getHtmlCode: function ($table) {
        var htmlCode;
        if ($table.length == 0 || $table[0].nodeName.toLowerCase() != "table")
            return false;
        $table.find("td,th").each(function () {
            var align = $(this).attr("align");
            if (!align) {
                align = $(this).css("text-align");
            }
            if (!align)
                align = "center";
            $(this).attr("align", align);
        });
        htmlCode = "<table>" + $table.html() + "</table>";
        return htmlCode;
    },
    getExcelFile: function ($table, fileName) {
        var htmlCode = this.getHtmlCode($table);
        if (!htmlCode) {
            $.tmsg("m_jfw", "表格生成错误，请联系管理员！", { infotype: 2 });
            return false;
        }
        htmlCode = escape(htmlCode)
        fileName = escape(fileName);
        var result;
        $.ajax({
            url: "/Home/CreateExcelByHtmlCode",
            type: "post",
            data: { "htmlCode": htmlCode, "sheetName": fileName },
            async: false,
            success: function (ret) {
                ret = $.parseJSON(ret);
                result = ret.Success;
                if (ret.Success) {
                    var url = "/Home/GetExcelFile?fullFileName=" + escape(ret.Message) + "&downloadName=" + fileName + "&r=" + Math.random();
                    //location.href = url; //不弹出新窗口，有些浏览器禁止弹窗；
                    window.open(url);
                } else {
                    $.tmsg("m_jfw", ret.Message, { infotype: 2 });
                }
            }
        });
        return result;
    }
};