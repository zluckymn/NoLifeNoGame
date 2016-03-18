var speed3 = 25;
var MyMar;
var demo, demo1, demo2;
function ScrollPic(pdivId, divid1, divid2, count) {
    if (document.getElementById(divid1)) {
        demo = document.getElementById(pdivId);
        demo1 = document.getElementById(divid1);
        demo2 = document.getElementById(divid2);
        var tbls = demo1.getElementsByTagName("table");
        var tbl, tr;
        if (tbls.length <= 0) return;
        tbl = tbls[0];
        if (tbl.rows[0].cells.length > count) {
            //$(demo1).find("td:first").html($(demo1).find("td:first").html() + $(demo1).find("td:first").html());
            demo2.innerHTML = demo1.innerHTML;
            MyMar = setInterval(Marquee, speed3);
            demo.onmouseover = function () { clearInterval(MyMar) }
            demo.onmouseout = function () { MyMar = setInterval(Marquee, speed3) }
        }
    }

}
var MarqueeAction = 0;
var MarqueeSpeed = 1;
function Marquee() {
    if (MarqueeAction == 0) {


        if (demo2.offsetWidth - demo.scrollLeft <= 0)
        { demo.scrollLeft -= demo1.offsetWidth; }
        else {
            demo.scrollLeft += MarqueeSpeed;
        }



    } else {


        if (demo.scrollLeft == 0) {
            demo.scrollLeft += demo1.offsetWidth;
        }
        else {
            demo.scrollLeft -= MarqueeSpeed;
        }


    }
}

function ScrollPicClass(count, id, url, type, width, callback) {
    this.url = url;
    this.id = id;
    var _this = this;
    this.speed3 = 25;
    this.MyMar = null;
    this.MarqueeAction = 0;
    this.MarqueeSpeed = 1;
    this.count = count;
    if (!width) { width = 880; }
    this.width = width;

    var htmlHead = '<table width="100%" border="0" cellspacing="0" cellpadding="0">';
    htmlHead += '<tr>';
    htmlHead += '<td width="28">';
    htmlHead += '<div class="scr_left">';
    htmlHead += '<a href="javascript:;" style="margin-top:0px;"></a></div>';
    htmlHead += '</td>';
    htmlHead += '<td style="padding: 5px 0 5px 0">';
    htmlHead += '<div id="demo3" style="overflow: hidden; height: 190px; width:' + width + 'px; margin-left: 5px;margin-right: 5px;">';
    htmlHead += '<table width="cellspacing=0">';
    htmlHead += '<tr>';
    htmlHead += '<td id="demo4" class="demo">';
    htmlHead += '<table width="100%" style="text-align: center">';
    htmlHead += '<tr>';

    var htmlfoot = '</tr></table>';
    htmlfoot += '</td>';
    htmlfoot += '<td id="demo5" class="demoCopy">';
    htmlfoot += '</td>';
    htmlfoot += '</tr>';
    htmlfoot += '</table>';
    htmlfoot += '</div>';
    htmlfoot += '</td>';
    htmlfoot += '<td width="28">';
    htmlfoot += '<div class="scr_right">';
    htmlfoot += '<a href="javascript:;" style="margin-top:0px;"></a></div>';
    htmlfoot += '</td>';
    htmlfoot += '</tr>';
    htmlfoot += '</table>';


    //    this.data = [{ "name": "120", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion" },
    //             { "name": "121", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion" },
    //             { "name": "122", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion" },
    //             { "name": "123", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion" },
    //             { "name": "124", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion" },
    //             { "name": "125", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion" },
    //             { "name": "126", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion" },
    //             { "name": "127", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion" },
    //             { "name": "128", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion" },
    //             { "name": "129", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion" },
    //             { "name": "130", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion"}];

    this.ScrollPic = function () {
        var tbls = $("#" + _this.id + " table td:eq(1)").find("div table");
        if (tbls.length <= 0) return;
        var tbl = $("#" + _this.id + " .scr_con");
        if (tbl.length > _this.count) {
            _this.tDivTd2.html(_this.tDivTd.html());
            _this.MyMar = setInterval(_this.Marquee, _this.speed3);
            _this.tDiv[0].onmouseover = function () { clearInterval(_this.MyMar); };
            _this.tDiv[0].onmouseout = function () { _this.MyMar = setInterval(_this.Marquee, _this.speed3); };
        }
    }

    this.Marquee = function () {
        if (_this.MarqueeAction == 0) {

            if (_this.tDivTd2[0].offsetWidth - _this.tDiv[0].scrollLeft <= 0) {
                _this.tDiv[0].scrollLeft = _this.tDiv[0].scrollLeft - _this.tDivTd[0].offsetWidth;
            }
            else {
                _this.tDiv[0].scrollLeft = _this.tDiv[0].scrollLeft + _this.MarqueeSpeed;
            }
        } else {

            if (_this.tDiv[0].scrollLeft == 0) {
                _this.tDiv[0].scrollLeft = _this.tDiv[0].scrollLeft + _this.tDivTd[0].offsetWidth;
            }
            else {
                _this.tDiv[0].scrollLeft = _this.tDiv[0].scrollLeft - _this.MarqueeSpeed;
            }
        }
    }

    this.load = function () {
        _this.tDiv = $("#" + _this.id + " table td:eq(1)").find("div");
        _this.tDivTd = $("#" + _this.id + " table td:eq(1)").find("div table td.demo");
        _this.tDivTd2 = $("#" + _this.id + " table td:eq(1)").find("div table td.demoCopy");
        _this.Marquee();
        _this.ScrollPic();
        $("#" + _this.id + " .scr_left")
        .mousedown(function () {
            _this.MarqueeAction = 0;
            _this.MarqueeSpeed = 10;
        }).mouseup(function () {
            _this.MarqueeSpeed = 1;
        });

        $("#" + _this.id + " .scr_right")
        .mousedown(function () {
            _this.MarqueeAction = 1;
            _this.MarqueeSpeed = 10;
        }).mouseup(function () {
            _this.MarqueeSpeed = 1;
        });
    }

    this.addContent = function () {
        if (type == 0) {
            $.ajax({
                url: _this.url,
                type: 'GET',
                dataType: 'json',
                timeout: 1000,
                cache: false,
                error: function (data) {
                    //window.location.reload();
                },
                success: function (data) {

                    var li = "";
                    var html = "";
                    html += htmlHead;
                    if (data.length == 0) {
                        li += "<td>";
                        li += '<div style="text-align: center; padding-bottom: 30px; padding-left: 0px; padding-right: 0px; padding-top: 30px;">';
                        li += '<img src=' + path + '/Content/Images/zh-cn/designrep/pic-erro.jpg complete="complete" width:/>';
                        li += '</div>';
                        li += '</td>';
                        //$("#data1").html(li);
                        html += li; //$("#data1").html();
                    } else {
                        $(data).each(function (i) {
                            li += "<td style='vertical-align:top'>";
                            li += '<div style="float: left; padding: 5px; margin: 5px" class="3">';
                            li += '<div class="scr_con">';
                            li += '<img  src=' + path + '"' + data[i].m + '" onerror="setTypeImg(this, \'um\', \'' + data[i].ext + '\')" onclick="ReadOnlineCustom(\'' + data[i].docid + '\',\'' + data[i].lastVersion + '\',0)" style="cursor: hand" /></div>';
                            li += '<div style="padding-top: 5px; text-align: center" docid=' + data[i].docid + ' lastVersion=' + data[i].lastVersion + ' name="' + data[i].name + '">';
                            li += '<a href="javascript:;" onclick="ReadOnlineCustom(\'' + data[i].docid + '\',\'' + data[i].lastVersion + '\',0)" class="blue">' + getSubStrLen(data[i].name, 30, "..") + '</a></div>';
                            li += '</div>';
                            li += '</td>';
                        });
                        html += li;
                    }
                    html += htmlfoot;
                    $("#" + _this.id).append(html);
                    if (callback) callback();
                    _this.load();
                }
            });
        }
        if (type == 1) {

            var lee = "";
            var htmlAll = "";
            htmlAll += htmlHead;
            lee += "<td >";
            lee += '<div style="text-align: center; padding-bottom: 30px; padding-left: 0px; padding-right: 0px; padding-top: 30px;">';
            lee += '<img src=' + path + '/Content/Images/pic/pic-erro.jpg complete="complete" width:/>';
            lee += '</div>';
            lee += '</td>';
            if ($("#" + _this.url).find("td").length == 0) { $("#" + _this.url).html(lee); }


            htmlAll += $("#" + _this.url).html();
            //$("#" + _this.url).html();
            htmlAll += htmlfoot;
            $("#" + _this.id).append(htmlAll);
            _this.load();

            if ($("#" + _this.url).find("td").length <= _this.count) {
                $("#" + _this.id).find("div.scr_left").parent().hide();
                $("#" + _this.id + " .scr_right").parent().hide();
            }
        }

        if (type == 2) {
            _this.load();

        }
    }
    this.addContent();
}


function ScrollPicClassType(count, classType, url, type, width, callback) {
    this.url = url;
    this.classType = classType;
    var _this = this;
    this.speed3 = 25;
    this.MyMar = null;
    this.MarqueeAction = 0;
    this.MarqueeSpeed = 1;
    this.count = count;
    if (!width) { width = 880; }
    this.width = width;

    var htmlHead = '<table width="100%" border="0" cellspacing="0" cellpadding="0">';
    htmlHead += '<tr>';
    htmlHead += '<div align="center" class="turnleft" style="display:none">';
    htmlHead += '</div>';
    htmlHead += '<td style="padding: 5px 0 5px 0">';
    htmlHead += '<div id="demo3" style="overflow: hidden; height: 180px; width:' + width + 'px; margin-left: 5px;margin-right: 5px;">';
    htmlHead += '<table width="cellspacing=0">';
    htmlHead += '<tr>';
    htmlHead += '<td id="demo4">';
    htmlHead += '<table width="100%" style="text-align: center">';
    htmlHead += '<tr>';

    var htmlfoot = '</table>';
    htmlfoot += '</td>';
    htmlfoot += '<td id="demo5">';
    htmlfoot += '</td>';
    htmlfoot += '</tr>';
    htmlfoot += '</table>';
    htmlfoot += '</div>';
    htmlfoot += '</td>';
    htmlfoot += '<div align="center" class="turnright" style="display:none">';
    htmlfoot += '</div>';
    htmlfoot += '</tr>';
    htmlfoot += '</table>';


    //    this.data = [{ "name": "120", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion" },
    //             { "name": "121", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion" },
    //             { "name": "122", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion" },
    //             { "name": "123", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion" },
    //             { "name": "124", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion" },
    //             { "name": "125", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion" },
    //             { "name": "126", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion" },
    //             { "name": "127", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion" },
    //             { "name": "128", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion" },
    //             { "name": "129", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion" },
    //             { "name": "130", "fileld": "fileld", "image": "image", "lastVersion": "lastVersion"}];

    this.ScrollPic = function () {
        $("." + _this.classType).each(function () {
            var tbls = $(this).find("table td:eq(1)").find("div table");
            if (tbls.length <= 0) return;
            var tbl = $(this).find(".picdivm");
            if (tbl.length > _this.count) {
                _this.tDivTd2.html(_this.tDivTd.html());
                _this.MyMar = setInterval(_this.Marquee, _this.speed3);
                _this.tDiv[0].onmouseover = function () { clearInterval(_this.MyMar); };
                _this.tDiv[0].onmouseout = function () { _this.MyMar = setInterval(_this.Marquee, _this.speed3); };
            }
        });
    }

    this.Marquee = function () {
        if (_this.MarqueeAction == 0) {

            if (_this.tDivTd2[0].offsetWidth - _this.tDiv[0].scrollLeft <= 0) {
                _this.tDiv[0].scrollLeft = _this.tDiv[0].scrollLeft - _this.tDivTd[0].offsetWidth;
            }
            else {
                _this.tDiv[0].scrollLeft = _this.tDiv[0].scrollLeft + _this.MarqueeSpeed;
            }
        } else {

            if (_this.tDiv[0].scrollLeft == 0) {
                _this.tDiv[0].scrollLeft = _this.tDiv[0].scrollLeft + _this.tDivTd[0].offsetWidth;
            }
            else {
                _this.tDiv[0].scrollLeft = _this.tDiv[0].scrollLeft - _this.MarqueeSpeed;
            }
        }
    }

    this.load = function () {
        $("." + _this.classType).each(function () {
            _this.tDiv = $(this).find("table td:eq(0)").find("div");

            _this.tDivTd = $(this).find("table td:eq(0)");
            _this.tDivTd2 = $(this).find("table td:eq(0)");
            _this.Marquee();
            _this.ScrollPic();

            $(this).find(".turnleft")
            .mousedown(function () {
                _this.MarqueeAction = 0;
                _this.MarqueeSpeed = 10;
            }).mouseup(function () {
                _this.MarqueeSpeed = 1;
            });

            $(this).find(".turnright")
            .mousedown(function () {
                _this.MarqueeAction = 1;
                _this.MarqueeSpeed = 10;
            }).mouseup(function () {
                _this.MarqueeSpeed = 1;
            });

        });

    }
    $("." + _this.classType).each(function () {

        $(this).mouseover(function () {

            $(this).find(".turnleft").show().end().find(".turnright").show();

        }).mouseout(function () {
            $(this).find(".turnleft").hide().end().find(".turnright").hide();
        })
    });

    this.addContent = function () {
        if (type == 0) {
            $.ajax({
                url: _this.url,
                type: 'GET',
                dataType: 'json',
                timeout: 1000,
                cache: false,
                error: function (data) {
                    //window.location.reload();
                },
                success: function (data) {

                    var li = "";
                    var html = "";
                    html += htmlHead;
                    if (data.length == 0) {
                        li += "<td>";
                        li += '<div style="text-align: center; padding-bottom: 30px; padding-left: 0px; padding-right: 0px; padding-top: 30px;">';
                        li += '<img src=' + path + '"/Content/Images/zh-cn/designrep/pic-erro.jpg" complete="complete" width:/>';
                        li += '</div>';
                        li += '</td>';
                        //$("#data1").html(li);
                        html += li; //$("#data1").html();
                    } else {
                        $(data).each(function (i) {
                            li += "<td style='vertical-align:top'>";
                            li += '<div style="float: left; padding: 5px; margin: 5px" class="3">';
                            li += '<div class="picdivm">';
                            li += '<img  src=' + path + '"' + data[i].m + '" onerror="setTypeImg(this, \'um\', \'' + data[i].ext + '\')" onclick="ReadOnlineCustom(\'' + data[i].docid + '\',\'' + data[i].lastVersion + '\',0)" style="cursor: hand" /></div>';
                            li += '<div style="padding-top: 5px; text-align: center" docid=' + data[i].docid + ' lastVersion=' + data[i].lastVersion + ' name="' + data[i].name + '">';
                            li += '<a href="javascript:" onclick="ReadOnlineCustom(\'' + data[i].docid + '\',\'' + data[i].lastVersion + '\',0)" class="blue">' + getSubStrLen(data[i].name, 30, "..") + '</a></div>';
                            li += '</div>';
                            li += '</td>';
                        });
                        html += li;
                    }
                    html += htmlfoot;
                    $("." + _this.classType).append(html);
                    if (callback) callback();
                    _this.load();
                }
            });
        }
        if (type == 1) {
            var lee = "";
            var htmlAll = "";
            htmlAll += htmlHead;
            lee += "<td >";
            lee += '<div style="text-align: center; padding-bottom: 30px; padding-left: 0px; padding-right: 0px; padding-top: 30px;">';
            lee += '<img src= ' + path + '"/Content/Images/zh-cn/designrep/pic-erro.jpg" complete="complete" width:/>';
            lee += '</div>';
            lee += '</td>';
            if ($("#" + _this.url).html().length == 0) { $("#" + _this.url).html(lee);}
          
            htmlAll += $("#" + _this.url).html();
            //$("#" + _this.url).html();
            htmlAll += htmlfoot;
            $("." + _this.classType).append(htmlAll);
            _this.load();
        }

        if (type == 2) {
            _this.load();

        }
    }
    this.addContent();

}
