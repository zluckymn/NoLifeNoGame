//点击定义交付物成果按钮弹出交付物设置弹窗

function dtDeliverables(taskId, tt) {
    var _title = String(taskId).indexOf(',') != -1 ? '批量定义交付物成果' : '定义交付物成果';
    box("/Projects/Assignment/PredefineFile?taskId=" + taskId + "&r=" + Math.random(), { boxid: 'dtd', title: _title, contentType: 'ajax', width: 800, submit_BtnName: '批量保存', cancel_BtnName: '关闭', modal: true,
    onLoad: function(o) {
        //当用户为金地和龙光的时候不执行change事件
        if ($("#checkCustomer").length == 0 || $("#checkCustomer").val() == 0) {
                o.db.find("#projProfId").change(function() {
                    $.get("/Projects/Assignment/GetProjPhase?r=" + Math.random() + "&projProfId=" + $(this).val(), function(data) {
                        if (data.success) {
                            o.db.find("#projPhaseId").html("<option value=0>请选择</option>");
                            var appendHtml = "";
                            for (var x = 0; x < data.items.length; x++) {
                                appendHtml += "<option value='" + data.items[x].Value + "'>" + data.items[x].Text + "</option>";
                            }
                            o.db.find("#projPhaseId").append(appendHtml);
                        }
                    }, "json");
                });
            }
            o.db.find("#BtnaddGroup").click(function() {
                if (o.db.find("#projProfId").val() == 0) { $.tmsg("m_dfjfw", "请选择专业"); return false; }
                if (o.db.find("#projPhaseId").val() == 0) { $.tmsg("m_dfjfw", "请选择阶段"); return false; }
                if (o.db.find("#fileCatId").val() == 0) { $.tmsg("m_dfjfw", "请选择类别"); return false; }
                var projPhareId = o.db.find("#projPhaseId").val(), projProfId = o.db.find("#projProfId").val(), fileCatId = o.db.find("#fileCatId").val(),
					description = o.db.find("#projProfId option:selected").text() + "+" + o.db.find("#projPhaseId option:selected").text() + "+" + o.db.find("#fileCatId option:selected").text();
                $.ajax({
                    url: "/Projects/Assignment/SaveCombination?r=" + Math.random(),
                    data: "projPhareId=" + projPhareId + "&projProfId=" + projProfId + "&fileCatId=" + fileCatId + "&taskId=" + taskId,
                    success: function(data) {
                        if (data.success) {
                            $.tmsg("m_dfjfw", "添加成功！", { infotype: 1 });
                            var ob = {}, _ids = String(data.id + '').replace(/,$/, "");
                            ob['tid'] = _ids;
                            ob['tidsp'] = _ids.replace(/,/g, '_');
                            ob['description'] = description;
                            ob['projProfId'] = projProfId; ob['projPhareId'] = projPhareId; ob['fileCatId'] = fileCatId;
                            var tpl = '<tr id="tr{tidsp}"><td align="center" style="line-height: 18px; padding: 5px 3px;" width="377"><font class="fontblue">{description}</font> <img src="/Content/Images/zh-cn/task/btn-del.png" style="cursor:pointer;" onclick="DeleteCombination(\'{tid}\', this);" alt="删除该组合" /><br /><input onclick="AllowUploadFree(this,\'{tid}\');" type="checkbox" name="isForce" checked value="" />允许自由上传</td><td class="nopadding"><table id="tb{tidsp}"></table><div style="background-color:#ffffcd; text-align:center; height:30px;"><a href="javascript:;" onclick="addBizFile(\'{tid}\',{projProfId},{projPhareId},{fileCatId});" class="gray">添加预定义文档</a></div></td></tr>';
                            tpl = tpl.Tpl(ob);
                            $("#zyjdlist").append(tpl);
                        } else {
                            $.tmsg("m_dfjfw", data.msg, { infotype: 2 });
                        }
                    },
                    dataType: "json"
                });
            });
            o.db.find("#CloseWindow").click(function() {
                o.destroy();
            });
        },
        submit_cb: function(o) {
            return SaveAdd(o, taskId, tt);
        },
        cancel_cb: function(o) {
            if (tt) {
                //window.location.reload();
                if (typeof (newProjectTaskView) != "undefined" && newProjectTaskView) {
                    $("#tdTaskDetail").a3Load("/Projects/Assignment/CtrlTaskDetailEx/" + taskId + "?r=" + Math.random());
                } else {
                    $("#tdTaskDetail").a3Load("/Projects/Assignment/CtrlTaskDetail/" + taskId + "?r=" + Math.random());
                }
            }
        }, onDestroy: function(o) {
            if (tt) {
                if (typeof (newProjectTaskView) != "undefined" && newProjectTaskView) {
                    $("#tdTaskDetail").a3Load("/Projects/Assignment/CtrlTaskDetailEx/" + taskId + "?r=" + Math.random());
                } else {
                    $("#tdTaskDetail").a3Load("/Projects/Assignment/CtrlTaskDetail/" + taskId + "?r=" + Math.random());
                }
            }

        }
    });
}
function SaveAdd(o, taskId, tt){ // 保存新批量添加
	var nd = o.db.find("input[ndfill='1']"), inpts = o.db.find("input[ndfill]"), ndf = false, tmp = "", dt = [], crh = false;
	nd.each(function(){
		if($.trim($(this).val()) == ""){
			ndf = true;
			return false;
		}
	});
	if(ndf){
		$.tmsg("m_dfjfw", "请填写预定义<font color='blue'>文档名称 和 扩展名</font>！");
		return false;
	}
	function __getFE(obj){
		var _oname = $(obj).find("td:first"), _oext = $(obj).find("td:eq(1)"),
		_fname = _oname.text(), _fext = _oext.text();
		if(_oname.find("input").length){
			_fname = _oname.find("input:first").val();
		}
		if(_oext.find("input").length){
			_fext = _oext.find("input:first").val();
		}
		_fname = $.trim(_fname+"").toLowerCase();
		_fext = $.trim(_fext+"").toLowerCase();
		return {name:_fname, ext: _fext}
	}
	o.db.find("table[id^='tb']").each(function(){
		var G = $.makeArray($(this).find("tr")), f1 = false;
		while(0 !== G.length){
			var ntr = G.shift(), ne;
			ne = __getFE(ntr);
			$.each(G, function(i, n){
				var _ne = __getFE(n);
				if(_ne.name === ne.name && _ne.ext === ne.ext){
					f1 = true;
					return false;
				}
			});
			if(f1){
				crh = true;
				continue;
			}
		}
	});
	if(crh){
		$.tmsg("m_dfjfw", "同 专业/阶段/属性 下不允许出现相同的预定义文档<br/>请检查...");
		return false;
	}
	inpts.each(function(){
		this.name = this.name.replace(/fileList\[\d{1,}\]/ig, "{#}");
	});
	var k = 0;
	for(var i = 0; i < inpts.length; i += 5){
		for(var j = i; j < 5 + i; ++j){
			var tag = YH.$id(inpts[j]);
			tmp = tag.name.replace("{#}", "fileList["+k+"]");
			tag.name = tmp;
			dt.push(tmp + "=" + $.trim($(tag).val()));
		}
		k++;
	}
	dt = dt.join("&");
	if(dt){
		var ser = $("#setJFWForm").serialize();
		$.ajax({
		    type:'POST',
			url: "/Projects/Assignment/SaveDefineFile",
			data: dt + "&" + ser + "&taskId=" + taskId,
			success: function(data) {
				if (data.success) {
					$.tmsg("m_dfjfw", "添加成功！loading...",{infotype:1});
				}else{
					$.tmsg("m_dfjfw", data.msg,{infotype:2});
				}
				dtDeliverables(taskId, tt);
			}
		});
	}
	//return false;
}

function DeleteCombination(id, o) {
	var txt = $(o).prev().text();
	if(confirm("确定删除 “"+txt+"” 组合吗？")){
		$.ajax({
			url: "/Projects/Assignment/DeleteCombination?r=" + Math.random(),
			data: "taskValueId=" + id,
			success: function(data) {
				if (data.success) {
					$.tmsg("m_dfjfw", "删除成功",{infotype:1});
					$("#tr"+String(id).replace(/,/g, '_')).remove();
				}else{
					$.tmsg("m_dfjfw", data.msg,{infotype:2});
				}
			},
			dataType: "json"
		});
	}
}

function AllowUploadFree(o, id) {
    var type = 0;
    if ($(o).attr("checked")) { type = 1; } else { type = 0; }
    $.ajax({
        url: "/Projects/Assignment/AllowUploadFree?r=" + Math.random(),
        data: "taskValueId=" + id + "&Type=" + type,
        success: function(data) {
            if (data.success) {
                $.tmsg("m_dfjfw", "设置成功",{infotype:1});
            }else{
				$.tmsg("m_dfjfw", data.msg,{infotype:2});
			}
        },
        dataType: "json"
    });
}

function addBizFile(id,projProfId,projPhareId,fileCatId) {
    var html = '';
    html += '<tr>';
    html += '<td height="29" width="200">';
    html += '<input class="inputborder" tid="'+id+'" ndfill="1" style="width: 180px;" type="text" name="{#}.name" value="" />';
    html += '</td>';
    html += '<td align="center" width="100">';
    html += '<input class="inputborder" tid="'+id+'" ndfill="1" style="width: 50px;" type="text" name="{#}.ext" value="" /><input type="hidden" ndfill="0" name="{#}.projProfId" value="'+projProfId+'" /><input type="hidden" ndfill="0" name="{#}.projPhareId" value="'+projPhareId+'" /><input type="hidden" ndfill="0" name="{#}.fileCatId" value="'+fileCatId+'" />';
    html += '</td>';
    html += '<td align="center" width="80" style="border-right:0px none">';
    html += '<a href="javascript:;" class="red">移除</a>';
    html += '</td>';
    html += '</tr>';
    html = $(html);
    html.find("a:last").click(function() {
		var o = html[0];
		if (o.parentNode){
			o.parentNode.removeChild(o);
		}
        //html.remove()
    });
    var trid = "#tb"+String(id).replace(/,/g, '_');
    $(trid).append(html);
}

function deleteBizFile(id, o) {
    $.ajax({
        url: "/Projects/Assignment/DeleteDefineFile?r=" + Math.random(),
        data: "preId=" + id,
        success: function(data) {
            if (data.success) {
                $.tmsg("m_dfjfw", "删除成功",{infotype:1});
                $(o).parent().parent().remove();
            }else{
				$.tmsg("m_dfjfw", data.msg,{infotype:2});
			}
        },
        dataType: "json"
    });
}

function editFname(o,prefineId,taskDefaultValId){
	var v = $(o).text(), inpt = $('<input class="inputborder" style="width: 180px;" type="text" name="name" value="'+v+'" />'), nv = "", ext = $(o).next().text();
	inpt.blur(function(){
		nv = $(this).val();
		if(v == nv){
			$(o).text(v);
		}else{
			SaveBizFile(o, nv, ext, prefineId, taskDefaultValId,1);
		}
	});
	$(o).html(inpt);
	inpt.focus();
}
function editExt(o,prefineId,taskDefaultValId){
	var v = $(o).text(), inpt = $('<input class="inputborder" style="width: 50px;" type="text" name="name" value="'+v+'" />'), nv = $(o).prev().text(), ext = "";
	inpt.blur(function(){
		ext = $(this).val();
		if(v == ext){
			$(o).text(v);
		}else{
			SaveBizFile(o, nv, ext, prefineId, taskDefaultValId,2);
		}
	});
	$(o).html(inpt);
	inpt.focus();
}
function SaveBizFile(o, nv, ext, prefineId, taskDefaultValId,tp) {
    $.ajax({
		type:'POST',
        url: "/Projects/Assignment/UpdateSingleDefFile",
        data: "prefineId=" + prefineId + "&name=" + escape(nv) + "&ext=" + ext,
        success: function(data) {
            if (data.success) {
				$.tmsg("m_dfjfw", "保存成功！", {infotype:1});
            }else{
				$.tmsg("m_dfjfw", data.msg, {infotype:2});
			}
			$(o).text(tp == 1 ? nv : ext);
        },
        dataType: "json"
    });
}