/*
*
* EditTable 1.0 
* 传入四个参数
*NullTips -- 空数据输出
*EditClass -- 在所需要编辑的列增加Class属性
*NumOnlyClass -- 其中列只能输入数字加Num属性
*ChangeClass  -- 更改内容后所改的样式
*
*  target
* 1、可排序 分页
* 2、 快速筛选
* 3、 二次开发
*/

(function ($) {
    $.fn.EditTable = function (options) {
        //初始化
        options = $.extend(true, {
            NullTips: "单击可编辑...",
            EditClass: "edit",
            NumOnlyClass: "Num",
            ChangeClass: "taEidtAfter",
            SaveMode: 1, //保存模式	1:全部一起提交保存， 2:单个编辑后实时保存
            SaveURL: "",
            SaveData: null, //可以是方法也可以是变量，保存模式1的方法可以用ChangeClass来获取当前编辑区域
            IsReload: false,
            tableName: "",   //数据库表名A3
            Primarykey: 1, //数据库主键A3
            SaveType: "",
            AddAction: [[
				null, //插入函数的位置	AfterEdit AfterSave
				null	//插入的函数	function({editing:editing_td_jQuery_Object, savedata:savedata_json}) {}
			], [null, null]]
        }, options);

        var _Table = this;
        this.doaction = function (position, data) {
            for (i in options.AddAction) {
                if (options.AddAction[i][0] == position && typeof options.AddAction[i][1] == "function") {
                    options.AddAction[i][1]({
                        editing: data.editing,
                        savedata: data.savedata
                    });
                }
            }
        }

        this.Numonly = function (obj) {
            if (event.keyCode == 46) {
                if ($(this).val().indexOf(".") != -1) {
                    return false;
                }
            } else {
                return event.keyCode >= 45 && event.keyCode <= 57;
            }

        }


        this.editTable = function () {
            var NullTips = options.NullTips;

            var EditClass = options.EditClass;
            var NumOnlyClass = options.NumOnlyClass;
            var emptyText = "<font color=silver>" + NullTips + "</font>";

            _Table.find("." + EditClass).each(function () {
                var tdCon = $(this).find("div").html();
                var tdCon_format = $.trim(tdCon.replace(/&nbsp;/ig, ""));
                if (tdCon_format == "" || typeof tdCon_format == "undefined") {
                    tdCon = emptyText;
                }
                var Content = "<textarea style='display:none;overflow:auto;width:96%;height:60px;'>" + tdCon_format + "</textarea><span>" + tdCon + "</span>";
                $(this).find("div").html(Content);
            });

            if (NumOnlyClass != null) {
                $("." + NumOnlyClass + " textarea").bind("keypress", _Table.Numonly).css("ime-mode", "Disabled");
            }



            _Table.find("." + EditClass).live("click", function () {
                if ($(this).find("textarea").css("display") == "none") {
                    $(this).find("span").hide();
                    if ($(this).find("span").html().indexOf(NullTips) != -1) {
                        $(this).find("textarea").html("");
                    } else {

                        var aa = $.trim($(this).find("span").html().replace(/&nbsp;/ig, ""));
                        $(this).find("textarea").html(aa);
                    }
                    $(this).find("textarea").show().focus(function () {
                        $(this).select();

                    }).focus();
                }

            });

            //
            _Table.find("." + EditClass + " textarea").live('blur', function () {
                var _that = $(this).parent().find("span").first();
                var _this = $(this).parent().find("span").html();
                var value = $.trim(this.value);
                if ((_this.indexOf(NullTips) == -1 && value != _this) || (_this.indexOf(NullTips) != -1 && value != "")) {

                    $(this).parent().addClass(options.ChangeClass);
                    _that.html(value);
                    if (options.SaveMode == 1) {
                        _Table.saveBar();
                    } else if (options.SaveMode == 2) {
                        var savedata = typeof options.SaveData == 'function' ? options.SaveData() : options.SaveData;

                        $.ajax({
                            url: options.SaveURL,
                            type: options.SaveType,
                            dataType: 'json',
                            data: savedata,
                            success: function (data) {
                                if (data.Success) {
                                    hiOverAlert("保存成功");
                                    _Table.doaction("AfterSave", { savedata: savedata });
                                    $("." + options.ChangeClass).removeClass(options.ChangeClass);
                                } else {
                                    hiAlert("保存失败");
                                }
                            }
                        });
                    } else if (options.SaveMode == 3) {
                        var key = $(this).parent().parent().find("input").attr("name");
                        var valId = $(this).parent().parent().find("input").attr("valueid");

                        if (options.tableName != "") {
                            $.ajax({
                                url: options.SaveURL,
                                type: 'post',
                                data: {
                                    tbName: options.tableName,
                                    queryStr: "db." + options.tableName + ".distinct('_id',{'" + options.Primarykey + "':'" + valId + "'})",
                                    dataStr: key + "=" + encodeURIComponent(value)
                                },
                                dataType: 'json',
                                error: function () {
                                    $.tmsg("m_jfw", "未知错误，请联系服务器管理员，或者刷新页面重试", { infotype: 2 });
                                },
                                success: function (data) {
                                    if (data.Success == false) {
                                        $.tmsg("m_jfw", data.Message, { infotype: 2 });
                                    } else {
                                        $("." + options.ChangeClass).removeClass(options.ChangeClass);
                                        $.tmsg("m_jfw", "保存成功！", { infotype: 1, time_out: 500 });
                                        _Table.doaction("AfterSave", { savedata: savedata });

                                    }
                                }
                            });
                        }

                    }
                    else if (options.SaveMode == 4) {
                        var key = $(this).parent().parent().find("input").attr("name");
                        var valId = $(this).parent().parent().find("input").attr("valueid");

                        if (options.tableName != "") {
                            $.ajax({
                                url: options.SaveURL,
                                type: 'post',
                                data: {
                                    tbName: options.tableName,
                                    queryStr: "db." + options.tableName + ".distinct('_id',{'" + options.Primarykey + "':'" + valId + "'})",
                                    dataStr: key + "=" + value
                                },
                                dataType: 'json',
                                error: function () {
                                    $.tmsg("m_jfw", "未知错误，请联系服务器管理员，或者刷新页面重试", { infotype: 2 });
                                },
                                success: function (data) {
                                    if (data.Success == false) {
                                        $.tmsg("m_jfw", data.Message, { infotype: 2 });
                                    } else {
                                        $("." + options.ChangeClass).removeClass(options.ChangeClass);
                                        $.tmsg("m_jfw", "保存成功！", { infotype: 1 });
                                        LoadDiv();
                                    }
                                }
                            });
                        }

                    }
                }
                if (value == "") {
                    _that.html(emptyText);
                }
                $(this).hide();
                _that.show();

                _Table.doaction("AfterEdit", { editing: $(this).parent() });

                // $("#btnupdate1").click();
            });
        }
        this.editTable();

        this.saveBar = function () {
            if ($("#savearea").css("display") == "none") {
                $("#savearea").show();
            }
        };

        if (options.SaveMode == 1) {
            var bottomSave = '<DIV style="Z-INDEX: 10000; POSITION: fixed; WIDTH: 100%; BOTTOM: 0px; DISPLAY: none; HEIGHT: 40px" id="savearea">' +
                            '<DIV style="BACKGROUND: none transparent scroll repeat 0% 0%; HEIGHT: 40px" id=meerkat-container jQuery1323502422512="26">' +
                            '<DIV class="baoCun" style="TEXT-ALIGN: center; FILTER: ; ZOOM: 1" id=SeriesStepSaveButton class=baoCun jQuery1323502422512="27">' +
                            '<INPUT style="WIDTH: 300px" value=数据已更新，请点此保存 type=button>' +
                            '</DIV></DIV></DIV>';
            $("body").append(bottomSave);
            $("#savearea input").click(function () {
                var savedata = typeof options.SaveData == 'function' ? options.SaveData() : options.SaveData;
                $.ajax({
                    url: options.SaveURL,
                    type: options.SaveType,
                    dataType: 'json',
                    data: savedata,
                    success: function (data) {
                        if (data.Success) {
                            hiOverAlert("保存成功");
                            $("#savearea").hide();
                            $("." + options.ChangeClass).removeClass(options.ChangeClass);
                            _Table.doaction("AfterSave", { savedata: savedata });
                            if (options.IsReload == true) {
                                window.location.reload();
                            }
                        } else {
                            hiAlert("保存失败");
                        }
                    }
                });
            });
        }

    }
})(jQuery);


var lockTableHead = (function () {
    var vph = YH.dom.getViewportHeight(), hasBind = false, sts = {}, gtableid = '', ids = [];
    function doLockHead(o, tableObj) {
        var tableid = tableObj.id, scr, $tb = $(tableObj),
            _rows = $tb.find('thead')[0].rows,
            h = 0, i = 0, _tr, of = $tb.offset();
        for (; _tr = _rows[i]; ++i) {
            h += parseInt(_tr.cells[0].offsetHeight, 10) || 24;
        }
        ids.push(tableid);
        sts['table_' + tableid] = { "tbl": tableObj, "fHeadId": 'scr_' + tableid, "height": h, "left": of.left, "top": of.top, "hasCalW": false, "tblht": tableObj.offsetHeight };
        scr = document.getElementById('scr_' + tableid);
        if (!scr) scr = createScr_(tableObj, tableid);
        gtableid = gtableid == '' ? tableid : gtableid;
        bindRS_();
    }
    function createScr_(tableObj, tableid) {
        var scr = $('<table cellpadding="0" style="margin-top:0px;" cellspacing="0" id="scr_' + tableid + '"><caption><div style="float:left"></div></caption><thead class="thead01"></thead><tbody></tbody></table>').appendTo(document.body),
            _th = scr.find('thead'), $tb = $(tableObj),
            ohead = $tb.find('thead').html();
        _th.html(ohead);
        scr = scr[0];
        if (tableObj.className != '') scr.className = tableObj.className;
        scr.style.display = 'none'; scr.style.zIndex = 999;

        YH.dom.elemFixedPos(scr);
        return scr;
    }
    function resetPos_(tbl) {
        var sct = $(document).scrollTop(), bt;
        if (tbl.top + tbl.height - 100 < sct && tbl.top + tbl.tblht - tbl.height / 2 > sct) {
            bt = 1;
        } else if (tbl.top + tbl.tblht < sct) {
            bt = 0;
        } else {
            bt = 0;
        }
        return bt;
    }
    function checkNowId() {
        var i = ids.length - 1, tbid, tbl, rid, r = 0;
        for (; i >= 0; i--) {
            tbid = ids[i];
            tbl = sts['table_' + tbid];
            rid = resetPos_(tbl);
            if (rid == 1) {
                r = tbid;
                break;
            }
        }
        return r;
    }
    function bindRS_() {
        if (hasBind) return;
        hasBind = true;
        $(window).bind("scroll", function () {
            var tbl = sts['table_' + gtableid],
                _b = resetPos_(tbl), scr = document.getElementById('scr_' + gtableid);
            if (_b == 0) {
                var _r = checkNowId();
                if (_r != 0) {
                    ;
                    gtableid = _r;
                    tbl = sts['table_' + gtableid];
                    _b = resetPos_(tbl); scr = document.getElementById('scr_' + gtableid);
                }
            }
            if (!tbl.hasCalW) {
                var r1 = $(tbl.tbl).find('thead').find("tr"),
                    cell, i = 0, _th, _tr0, _w, _cspan; scr.style.width = $(tbl.tbl).width() + 1 + 'px';

                r1.each(function (i) {
                    if (i == 0) {
                        for (var j = 0; j < $(this).find("th").length; j++) {
                            _w = $(this).find("th:eq(" + j + ")").width() + 2 + 'px';
                            $(scr).find('tr:first').find("th:eq(" + j + ")").css("width", _w);
                        }
                    } else {
                        for (var j = 0; j < $(this).find("th").length; j++) {
                            _w = $(this).find("th:eq(" + j + ")").width() - 1 + 'px';
                            $(scr).find('tr:eq(1)').find("th:eq(" + j + ")").css("width", _w);
                        }
                    }
                })
                tbl.hasCalW = true;
            }
            if (_b == 1) {
                scr.style.top = 0;
                scr.style.left = $(tbl.tbl).offset().left + 'px';
                scr.style.display = '';
            } else {
                scr.style.display = 'none';
            }
        }).bind("resize", function () {
            vph = YH.dom.getViewportHeight();
            var tbl = sts['table_' + gtableid], scr = document.getElementById('scr_' + gtableid);
            scr.style.left = $(tbl.tbl).offset().left;
        });
    }
    return {
        doLock: function (o, tableObj, h) {
            if (!tableObj) return;
            YH.dom.fixedPosition();
            doLockHead(o, tableObj);
        }
    };
})();





